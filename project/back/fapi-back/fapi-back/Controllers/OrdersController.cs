using Microsoft.AspNetCore.Mvc;
using fapi_back.Models;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ICatalogService _catalog;
    private readonly ICustomerStore _customers;
    private readonly IOrderStore _orders;
    private readonly IRateService _rates;

    public OrdersController(ICatalogService catalog, ICustomerStore customers, IOrderStore orders, IRateService rates)
    {
        _catalog = catalog;
        _customers = customers;
        _orders = orders;
        _rates = rates;
    }

   
    [HttpPost("quote")]
    public async Task<IActionResult> QuoteCart([FromBody] QuoteRequest req, CancellationToken ct)
    {
        if (req.Items is null || req.Items.Count == 0)
            return BadRequest(new { error = "Košík je prázdný." });

        req.TargetCurrency = (req.TargetCurrency ?? "CZK").Trim().ToUpperInvariant();
        if (req.TargetCurrency.Length != 3)
            return BadRequest(new { error = "Měna musí mít 3 znaky (CZK/EUR/...)." });

        decimal subtotal = 0m;
        
        // tady validuju položky a počítám mezisoučet
        foreach (var it in req.Items)
        {
            if (it.Quantity < 1 || it.Quantity > 999)
                return BadRequest(new { error = "Množství musí být 1–999." });

            var p = _catalog.FindProduct(it.ProductId);
            if (p is null)
                return BadRequest(new { error = $"Neplatný ProductId: {it.ProductId}" });

            subtotal += (decimal)p.price * it.Quantity;
        }
        
        // tady dopočítávám DPH a celkovou cenu v CZK
        var vat = Pricing.Round2(subtotal * Pricing.VatRate);
        var totalCzk = Pricing.Round2(subtotal + vat);
        
        // tady si stahuju kurzy z ČNB 
        var map = await _rates.GetRatesCzkPerUnitAsync(ct);
        if (!map.TryGetValue(req.TargetCurrency, out var rate))
            return BadRequest(new { error = $"Měna {req.TargetCurrency} není v kurzech ČNB." });
        
        // tady přepočítávám cenu do cílové měny
        var totalInCurrency = Pricing.Round2(totalCzk / rate);

        return Ok(new
        {
            subtotalCzk = Pricing.Round2(subtotal),
            vatCzk = vat,
            totalCzk,
            targetCurrency = req.TargetCurrency,
            rateCzkPerUnit = Pricing.Round2(rate),
            totalInCurrency
        });
    }
    // tvorba objedavky
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderRequest req, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();
        
        // ověřuju, že zákazník existuje
        if (!_customers.TryGet(req.CustomerId, out _))
            errors["customerId"] = "CustomerId neexistuje.";
        
        // ověřuju, že je něco v košíku
        if (req.Items is null || req.Items.Count == 0)
            errors["items"] = "Vyber alespoň jeden produkt.";
        
        // validuju jednotlivé položky
        foreach (var it in req.Items)
        {
            if (it.Quantity < 1 || it.Quantity > 999)
                errors["quantity"] = "Quantity musí být 1–999.";

            if (_catalog.FindProduct(it.ProductId) is null)
                errors["productId"] = $"Neplatný ProductId: {it.ProductId}";
        }

        req.TargetCurrency = (req.TargetCurrency ?? "CZK").Trim().ToUpperInvariant();
        if (req.TargetCurrency.Length != 3) errors["targetCurrency"] = "Měna musí mít 3 znaky (CZK/EUR/...).";

        if (errors.Count > 0) return BadRequest(new { errors });

        // výpočet z katalogu (backend je autorita - frontrnd nikdy nepočítá)
        decimal subtotal = 0m;
        foreach (var it in req.Items)
        {
            var p = _catalog.FindProduct(it.ProductId)!;
            subtotal += (decimal)p.price * it.Quantity;
        }

        var vat = Pricing.Round2(subtotal * Pricing.VatRate);
        var totalCzk = Pricing.Round2(subtotal + vat);

        var map = await _rates.GetRatesCzkPerUnitAsync(ct);
        if (!map.TryGetValue(req.TargetCurrency, out var rate))
            return BadRequest(new { errors = new { targetCurrency = $"Měna {req.TargetCurrency} není v kurzech ČNB." } });

        var totalInCurrency = Pricing.Round2(totalCzk / rate);

        // req.Total je int – uložíme zaokrouhlenou částku v cílové měně
       
        req.Total = (int)Math.Round(totalInCurrency, 0, MidpointRounding.AwayFromZero);

        var orderId = _orders.Save(req);
        return Ok(new { orderId, total = req.Total, currency = req.TargetCurrency });
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        //hledám objednávku
        if (!_orders.TryGet(id, out var o) || o is null)
            return NotFound(new { error = "Objednávka nenalezena." });

        return Ok(o);
    }
}

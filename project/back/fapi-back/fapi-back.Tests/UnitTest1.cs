using Microsoft.AspNetCore.Mvc;
using Xunit;
using fapi_back.Models;

// Pozn: Controllers + Service jsou u tebe v GLOBAL namespace,
// proto nepouûÌvej using fapi_back.Controllers / using fapi_back.Service.

public class OrdersControllerUnitTests
{
    [Fact]
    public async Task Create_WithValidCart_ReturnsOk()
    {
        // arrange - in-memory sluûby
        ICatalogService catalog = new CatalogService();
        ICustomerStore customers = new InMemoryCustomerStore();
        IOrderStore orders = new InMemoryOrderStore();
        IRateService rates = new FakeRateService();

        // vytvo¯ z·kaznÌka (aby CustomerId existovalo)
        var created = customers.Add(new Customer
        {
            Name = "Test User",
            Email = "test@test.cz",
            Phone = "123456789",
            Address = "Praha"
        });

        var controller = new OrdersController(
            catalog,
            customers, // <- 2. parametr je ICustomerStore
            orders,    // <- 3. parametr je IOrderStore
            rates      // <- 4. parametr je IRateService
        );

        var req = new OrderRequest
        {
            CustomerId = created.Id,
            TargetCurrency = "EUR",
            Items =
            {
                new OrderItemRequest { ProductId = 1, Quantity = 2 }
            }
        };

        // act
        var result = await controller.Create(req, CancellationToken.None);

        // assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        Assert.NotNull(ok.Value);
    }
}

// Fake kurz (aù netestujeö re·lnÈ »NB)
public sealed class FakeRateService : IRateService
{
    public Task<Dictionary<string, decimal>> GetRatesCzkPerUnitAsync(CancellationToken ct)
        => Task.FromResult(new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["CZK"] = 1m,
            ["EUR"] = 25m
        });
}

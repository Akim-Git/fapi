using Microsoft.AspNetCore.Mvc;
using fapi_back.Models;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerStore _store;
    public CustomersController(ICustomerStore store) => _store = store;

    [HttpPost]
    public IActionResult Create([FromBody] Customer c)
    {
        //kolekce chyb
        var errors = new Dictionary<string, string>();
        //validace
        if (string.IsNullOrWhiteSpace(c.Name) || c.Name.Trim().Length < 3) errors["fullName"] = "Zadejte jméno (min 3).";
        if (string.IsNullOrWhiteSpace(c.Email) || !c.Email.Contains("@")) errors["email"] = "Zadejte email.";
        if (string.IsNullOrWhiteSpace(c.Phone)) errors["phone"] = "Zadejte telefon.";
        if (string.IsNullOrWhiteSpace(c.Address) || c.Address.Trim().Length < 6) errors["address"] = "Zadejte adresu.";

        if (errors.Count > 0) return BadRequest(new { errors });

        var created = _store.Add(c);
        return Ok(new { id = created.Id });
    }
}

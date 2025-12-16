using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ICatalogService _catalog;

    // bery katalog
    public ProductsController(ICatalogService catalog) => _catalog = catalog;

    [HttpGet]
    public IActionResult Get() => Ok(_catalog.GetProducts());
}

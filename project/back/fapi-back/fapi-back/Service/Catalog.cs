using fapi_back.Models;

public interface ICatalogService
{
    IReadOnlyList<Product> GetProducts();
    Product? FindProduct(int id);
}

public sealed class CatalogService : ICatalogService
{
    private static readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Káva 250g", price = 199 },
        new Product { Id = 2, Name = "Hrnek",    price = 149 },
        new Product { Id = 3, Name = "Tričko",   price = 399 },
    };

    public IReadOnlyList<Product> GetProducts() => _products;
    public Product? FindProduct(int id) => _products.FirstOrDefault(p => p.Id == id);
}

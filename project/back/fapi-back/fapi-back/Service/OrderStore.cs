using System.Collections.Concurrent;
using fapi_back.Models;

public interface IOrderStore
{
    string Save(OrderRequest order);
    bool TryGet(string id, out OrderRequest? order);
}

public sealed class InMemoryOrderStore : IOrderStore
{
    private readonly ConcurrentDictionary<string, OrderRequest> _map = new();

    public string Save(OrderRequest order)
    {
        var id = Guid.NewGuid().ToString("N");
        _map[id] = order;
        return id;
    }

    public bool TryGet(string id, out OrderRequest? order)
    {
        var ok = _map.TryGetValue(id, out var o);
        order = o;
        return ok;
    }
}

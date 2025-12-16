using System.Collections.Concurrent;
using fapi_back.Models;

public interface ICustomerStore
{
    Customer Add(Customer c);
    bool TryGet(int id, out Customer? c);
}

public sealed class InMemoryCustomerStore : ICustomerStore
{
    private readonly ConcurrentDictionary<int, Customer> _map = new();
    private int _id = 0;

    public Customer Add(Customer c)
    {
        var id = Interlocked.Increment(ref _id);
        c.Id = id;
        _map[id] = c;
        return c;
    }

    public bool TryGet(int id, out Customer? c)
    {
        var ok = _map.TryGetValue(id, out var found);
        c = found;
        return ok;
    }
}

namespace fapi_back.Models
{
    // položka košíku (to posílá frontend)
    public sealed class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderRequest
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }

        // seznam ID produktů + množství
        public List<OrderItemRequest> Items { get; set; } = new();

        public string TargetCurrency { get; set; } = "EUR";
        public int Total { get; set; }
    }

    // DTO pro quote
    public sealed class QuoteRequest
    {
        public List<OrderItemRequest> Items { get; set; } = new();
        public string TargetCurrency { get; set; } = "CZK";
    }
}

using Microsoft.AspNetCore.Mvc;

namespace fapi_back.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int price { get; set; }
    }
    
}

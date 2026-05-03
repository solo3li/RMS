using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class OrderItemRequest
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Notes { get; set; }
    }

    public class OrderEditRequest
    {
        public int OrderId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace RMS.Web.Models
{
    public class OrderItem
    {
        public int ID { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public string? Notes { get; set; }
    }
}

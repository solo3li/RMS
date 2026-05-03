using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMS.Web.Models
{
    public class Order
    {
        public int ID { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public OrderType OrderType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        public OrderStatus Status { get; set; }

        public string? Notes { get; set; }

        public string CreatedByUserId { get; set; } = string.Empty;
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public string? DeliveryUserId { get; set; }
        public ApplicationUser? DeliveryUser { get; set; }

        public int? DeliveryAddressId { get; set; }
        public CustomerAddress? DeliveryAddress { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryFee { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

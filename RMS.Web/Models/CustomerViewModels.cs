using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class CustomerIndexViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpend { get; set; }
        public string? Tags { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public Customer Customer { get; set; } = null!;
        public int TotalOrders { get; set; }
        public decimal TotalSpend { get; set; }
        public List<Order> OrderHistory { get; set; } = new();
        public List<CustomerAddress> Addresses { get; set; } = new();
    }
}

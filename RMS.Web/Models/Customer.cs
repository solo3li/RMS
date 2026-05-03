using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class Customer
    {
        public int ID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string? Tags { get; set; }

        public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}

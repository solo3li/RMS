using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class CustomerAddress
    {
        public int ID { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        [Required]
        [MaxLength(250)]
        public string AddressLine { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Zone { get; set; }

        public bool IsDefault { get; set; }
    }
}

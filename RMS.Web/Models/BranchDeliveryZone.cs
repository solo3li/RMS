using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMS.Web.Models
{
    public class BranchDeliveryZone
    {
        public int ID { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ZoneName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryFee { get; set; }
    }
}

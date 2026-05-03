using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMS.Web.Models
{
    public class MenuItem
    {
        public int ID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public bool IsAvailableGlobal { get; set; } = true;

        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableTo { get; set; }

        public string? ImageUrl { get; set; }

        public ICollection<Extra> Extras { get; set; } = new List<Extra>();
        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
        public ICollection<BranchMenuItemAvailability> BranchAvailabilities { get; set; } = new List<BranchMenuItemAvailability>();
    }
}

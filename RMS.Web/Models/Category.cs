using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class Category
    {
        public int ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class SystemSetting
    {
        public int ID { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}

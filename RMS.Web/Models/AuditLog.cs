using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class AuditLog
    {
        public int ID { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public string EntityId { get; set; } = string.Empty;

        public string? BeforeValue { get; set; } // JSON

        public string? AfterValue { get; set; } // JSON

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

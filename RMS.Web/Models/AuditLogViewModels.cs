using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RMS.Web.Models
{
    public class AuditLogIndexViewModel
    {
        public List<AuditLog> AuditLogs { get; set; } = new();

        public string? UserId { get; set; }
        public string? EntityType { get; set; }
        public string? AuditAction { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public SelectList Users { get; set; } = null!;
        public SelectList EntityTypes { get; set; } = null!;
        public SelectList Actions { get; set; } = null!;
    }
}

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        // Many-to-Many relationship with Branch
        public ICollection<Branch> AssignedBranches { get; set; } = new List<Branch>();
    }
}

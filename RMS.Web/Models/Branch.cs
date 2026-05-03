using System.ComponentModel.DataAnnotations;

namespace RMS.Web.Models
{
    public class Branch
    {
        public int ID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsOpen { get; set; }

        public string WorkingHours { get; set; } = string.Empty;

        public string DeliveryZones { get; set; } = string.Empty;

        public ICollection<BranchDeliveryZone> BranchDeliveryZones { get; set; } = new List<BranchDeliveryZone>();

        // Many-to-Many relationship with ApplicationUser
        public ICollection<ApplicationUser> AssignedUsers { get; set; } = new List<ApplicationUser>();

        public ICollection<BranchMenuItemAvailability> MenuItemAvailabilities { get; set; } = new List<BranchMenuItemAvailability>();
        
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}

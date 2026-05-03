namespace RMS.Web.Models
{
    public class BranchMenuItemAvailability
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        public bool IsAvailable { get; set; }
    }
}

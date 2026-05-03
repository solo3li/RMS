using System.Collections.Generic;

namespace RMS.Web.Models
{
    public class BranchMenuViewModel
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public List<MenuItemBranchAvailabilityViewModel> MenuItems { get; set; } = new List<MenuItemBranchAvailabilityViewModel>();
    }

    public class MenuItemBranchAvailabilityViewModel
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool IsAvailableGlobal { get; set; }
        public bool IsAvailableInBranch { get; set; }
    }
}

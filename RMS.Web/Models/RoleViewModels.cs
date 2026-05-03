namespace RMS.Web.Models
{
    public class RoleViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int PermissionCount { get; set; }
    }

    public class RolePermissionsViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<RoleClaimViewModel> Permissions { get; set; } = new();
    }

    public class RoleClaimViewModel
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}

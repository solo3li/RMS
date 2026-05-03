using System.Collections.Generic;

namespace RMS.Web.Models
{
    public class PermissionViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<RoleClaimsViewModel> RoleClaims { get; set; } = new();
    }

    public class RoleClaimsViewModel
    {
        public string Value { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RMS.Web.Constants;
using RMS.Web.Models;
using System.Security.Claims;

namespace RMS.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var roles = _roleManager.Roles.ToList();
            var model = new List<RoleViewModel>();

            foreach (var role in roles)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                model.Add(new RoleViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? "",
                    PermissionCount = claims.Count(c => c.Type == CustomClaimTypes.Permission)
                });
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditPermissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            var rolePermissions = roleClaims.Where(c => c.Type == CustomClaimTypes.Permission).Select(c => c.Value).ToList();

            var allPermissions = Permissions.GetAllPermissions();

            var model = new RolePermissionsViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name ?? "",
                Permissions = allPermissions.Select(p => new RoleClaimViewModel
                {
                    Type = CustomClaimTypes.Permission,
                    Value = p,
                    Selected = rolePermissions.Contains(p)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditPermissions(RolePermissionsViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return NotFound();

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = claims.Where(c => c.Type == CustomClaimTypes.Permission).ToList();

            // Remove all existing permissions
            foreach (var claim in permissionClaims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            // Add selected permissions
            var selectedPermissions = model.Permissions.Where(p => p.Selected).ToList();
            foreach (var permission in selectedPermissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission.Value));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Models;
using System.Security.Claims;

namespace RMS.Web.Controllers
{
    [Authorize(Policy = Permissions.Users.Manage)]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public async Task<IActionResult> ManagePermissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null || role.Name == null)
            {
                return NotFound();
            }

            var model = new PermissionViewModel
            {
                RoleId = roleId,
                RoleName = role.Name
            };

            var allPermissions = Permissions.GetAllPermissions();
            var claims = await _roleManager.GetClaimsAsync(role);
            var allClaimValues = claims.Select(a => a.Value).ToList();

            model.RoleClaims = allPermissions.Select(p => new RoleClaimsViewModel
            {
                Value = p,
                Selected = allClaimValues.Contains(p)
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagePermissions(PermissionViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null)
            {
                return NotFound();
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            var selectedClaims = model.RoleClaims.Where(a => a.Selected).ToList();
            foreach (var claim in selectedClaims)
            {
                await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, claim.Value));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

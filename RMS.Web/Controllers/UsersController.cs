using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Models;

namespace RMS.Web.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Include(u => u.AssignedBranches)
                .ToListAsync();

            var userViewModels = new List<UserIndexViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserIndexViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    IsActive = user.IsActive,
                    Roles = roles,
                    Branches = user.AssignedBranches.Select(b => b.Name)
                });
            }

            return View(userViewModels);
        }

        [Authorize(Policy = Permissions.Users.Manage)]
        public async Task<IActionResult> Create()
        {
            var model = new CreateUserViewModel
            {
                RoleList = new MultiSelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name"),
                BranchList = new MultiSelectList(await _context.Branches.ToListAsync(), "ID", "Name")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Users.Manage)]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (model.SelectedRoles != null && model.SelectedRoles.Any())
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }

                    if (model.SelectedBranches != null && model.SelectedBranches.Any())
                    {
                        var branches = await _context.Branches
                            .Where(b => model.SelectedBranches.Contains(b.ID))
                            .ToListAsync();
                        user.AssignedBranches = branches;
                        await _userManager.UpdateAsync(user);
                    }

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            model.RoleList = new MultiSelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", model.SelectedRoles);
            model.BranchList = new MultiSelectList(await _context.Branches.ToListAsync(), "ID", "Name", model.SelectedBranches);
            return View(model);
        }

        [Authorize(Policy = Permissions.Users.Manage)]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.Users
                .Include(u => u.AssignedBranches)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                IsActive = user.IsActive,
                SelectedRoles = userRoles.ToList(),
                SelectedBranches = user.AssignedBranches.Select(b => b.ID).ToList(),
                RoleList = new MultiSelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", userRoles),
                BranchList = new MultiSelectList(await _context.Branches.ToListAsync(), "ID", "Name", user.AssignedBranches.Select(b => b.ID))
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Users.Manage)]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.Users
                    .Include(u => u.AssignedBranches)
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (user == null) return NotFound();

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.IsActive = model.IsActive;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update Roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (model.SelectedRoles != null)
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }

                    // Update Branches
                    user.AssignedBranches.Clear();
                    if (model.SelectedBranches != null)
                    {
                        var branches = await _context.Branches
                            .Where(b => model.SelectedBranches.Contains(b.ID))
                            .ToListAsync();
                        foreach (var branch in branches)
                        {
                            user.AssignedBranches.Add(branch);
                        }
                    }

                    await _userManager.UpdateAsync(user);

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            model.RoleList = new MultiSelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", model.SelectedRoles);
            model.BranchList = new MultiSelectList(await _context.Branches.ToListAsync(), "ID", "Name", model.SelectedBranches);
            return View(model);
        }
    }
}

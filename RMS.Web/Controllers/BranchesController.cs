using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Hubs;
using RMS.Web.Models;

namespace RMS.Web.Controllers
{
    [Authorize]
    public class BranchesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<RMS.Web.Hubs.OrderHub> _hubContext;

        public BranchesController(ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<RMS.Web.Hubs.OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Branches
        [Authorize(Policy = Permissions.Branches.View)]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Branches.ToListAsync());
        }

        // GET: Branches/Details/5
        [Authorize(Policy = Permissions.Branches.View)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(m => m.ID == id);
            if (branch == null)
            {
                return NotFound();
            }

            return View(branch);
        }

        // GET: Branches/Create
        [Authorize(Policy = Permissions.Branches.Manage)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Branches.Manage)]
        public async Task<IActionResult> Create([Bind("ID,Name,IsOpen,WorkingHours,DeliveryZones")] Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Add(branch);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // GET: Branches/Edit/5
        [Authorize(Policy = Permissions.Branches.Manage)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
            {
                return NotFound();
            }
            return View(branch);
        }

        // POST: Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Branches.Manage)]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,IsOpen,WorkingHours,DeliveryZones")] Branch branch)
        {
            if (id != branch.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(branch);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BranchExists(branch.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // GET: Branches/Delete/5
        [Authorize(Policy = Permissions.Branches.Manage)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(m => m.ID == id);
            if (branch == null)
            {
                return NotFound();
            }

            return View(branch);
        }

        // POST: Branches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Branches.Manage)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                _context.Branches.Remove(branch);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BranchExists(int id)
        {
            return _context.Branches.Any(e => e.ID == id);
        }

        // GET: Branches/ManageMenu/5
        [Authorize(Policy = Permissions.Branches.Manage)]
        public async Task<IActionResult> ManageMenu(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches
                .Include(b => b.MenuItemAvailabilities)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (branch == null)
            {
                return NotFound();
            }

            var menuItems = await _context.MenuItems
                .Include(m => m.Category)
                .ToListAsync();

            var viewModel = new BranchMenuViewModel
            {
                BranchId = branch.ID,
                BranchName = branch.Name,
                MenuItems = menuItems.Select(mi => new MenuItemBranchAvailabilityViewModel
                {
                    MenuItemId = mi.ID,
                    Name = mi.Name,
                    CategoryName = mi.Category.Name,
                    IsAvailableGlobal = mi.IsAvailableGlobal,
                    IsAvailableInBranch = branch.MenuItemAvailabilities
                        .FirstOrDefault(ba => ba.MenuItemId == mi.ID)?.IsAvailable ?? mi.IsAvailableGlobal
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Branches/ToggleMenuItemAvailability
        [HttpPost]
        [Authorize(Policy = Permissions.Branches.Manage)]
        public async Task<IActionResult> ToggleMenuItemAvailability(int branchId, int menuItemId, bool isAvailable)
        {
            var availability = await _context.BranchMenuItemAvailabilities
                .FirstOrDefaultAsync(ba => ba.BranchId == branchId && ba.MenuItemId == menuItemId);

            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            var branch = await _context.Branches.FindAsync(branchId);

            if (availability == null)
            {
                availability = new BranchMenuItemAvailability
                {
                    BranchId = branchId,
                    MenuItemId = menuItemId,
                    IsAvailable = isAvailable
                };
                _context.BranchMenuItemAvailabilities.Add(availability);
            }
            else
            {
                availability.IsAvailable = isAvailable;
            }

            await _context.SaveChangesAsync();

            // Notify via SignalR
            if (menuItem != null)
            {
                await _hubContext.Clients.All.SendAsync("StockStatusChanged", menuItemId, menuItem.Name, isAvailable, branch?.Name ?? "General");
            }

            return Json(new { success = true });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Hubs;
using RMS.Web.Models;


namespace RMS.Web.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<RMS.Web.Hubs.OrderHub> _hubContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MenuController(ApplicationDbContext context, 
            Microsoft.AspNetCore.SignalR.IHubContext<RMS.Web.Hubs.OrderHub> hubContext,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _hubContext = hubContext;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Menu
        [Authorize(Policy = Permissions.Menu.View)]
        public async Task<IActionResult> Index(int? categoryId)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;

            var menuItems = _context.MenuItems.Include(m => m.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                menuItems = menuItems.Where(m => m.CategoryId == categoryId.Value);
            }

            return View(await menuItems.ToListAsync());
        }

        // GET: Menu/CreateItem
        [Authorize(Policy = Permissions.Menu.Manage)]
        public IActionResult CreateItem()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "ID", "Name");
            return View();
        }

        // POST: Menu/CreateItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> CreateItem([Bind("ID,Name,Description,Price,CategoryId,IsAvailableGlobal,ImageUrl,AvailableFrom,AvailableTo")] MenuItem menuItem, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    menuItem.ImageUrl = await SaveImage(imageFile);
                }

                _context.Add(menuItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "ID", "Name", menuItem.CategoryId);
            return View(menuItem);
        }

        // GET: Menu/EditItem/5
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> EditItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "ID", "Name", menuItem.CategoryId);
            return View(menuItem);
        }

        // POST: Menu/EditItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> EditItem(int id, [Bind("ID,Name,Description,Price,CategoryId,IsAvailableGlobal,ImageUrl,AvailableFrom,AvailableTo")] MenuItem menuItem, IFormFile? imageFile)
        {
            if (id != menuItem.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        menuItem.ImageUrl = await SaveImage(imageFile);
                    }

                    _context.Update(menuItem);
                    await _context.SaveChangesAsync();

                    // Notify via SignalR
                    await _hubContext.Clients.All.SendAsync("StockStatusChanged", menuItem.ID, menuItem.Name, menuItem.IsAvailableGlobal, "Global");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MenuItemExists(menuItem.ID))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "ID", "Name", menuItem.CategoryId);
            return View(menuItem);
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "menu");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/uploads/menu/" + uniqueFileName;
        }

        // GET: Menu/ManageOptions/5
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> ManageOptions(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.MenuItems
                .Include(m => m.Extras)
                .Include(m => m.Variants)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        // POST: Menu/AddExtra
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> AddExtra(int menuItemId, string name, decimal price)
        {
            var extra = new Extra
            {
                MenuItemId = menuItemId,
                Name = name,
                Price = price
            };

            _context.Extras.Add(extra);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageOptions), new { id = menuItemId });
        }

        // POST: Menu/DeleteExtra/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> DeleteExtra(int id)
        {
            var extra = await _context.Extras.FindAsync(id);
            if (extra != null)
            {
                int menuItemId = extra.MenuItemId;
                _context.Extras.Remove(extra);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageOptions), new { id = menuItemId });
            }
            return NotFound();
        }

        // POST: Menu/AddVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> AddVariant(int menuItemId, string name, decimal price)
        {
            var variant = new Variant
            {
                MenuItemId = menuItemId,
                Name = name,
                Price = price
            };

            _context.Variants.Add(variant);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageOptions), new { id = menuItemId });
        }

        // POST: Menu/DeleteVariant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            var variant = await _context.Variants.FindAsync(id);
            if (variant != null)
            {
                int menuItemId = variant.MenuItemId;
                _context.Variants.Remove(variant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageOptions), new { id = menuItemId });
            }
            return NotFound();
        }

        // GET: Menu/CreateCategory
        [Authorize(Policy = Permissions.Menu.Manage)]
        public IActionResult CreateCategory()
        {
            return View();
        }

        // POST: Menu/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Menu.Manage)]
        public async Task<IActionResult> CreateCategory([Bind("ID,Name")] Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        private bool MenuItemExists(int id)
        {
            return _context.MenuItems.Any(e => e.ID == id);
        }
    }
}

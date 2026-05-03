using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Models;

namespace RMS.Web.Controllers
{
    [Authorize(Policy = Permissions.System.ManageSettings)]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Dictionary<string, string> settings)
        {
            foreach (var setting in settings)
            {
                var dbSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == setting.Key);
                if (dbSetting != null)
                {
                    dbSetting.Value = setting.Value ?? "";
                }
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

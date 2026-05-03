using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Models;

namespace RMS.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = Permissions.System.ViewAuditLogs)]
        public async Task<IActionResult> AuditLogs(string? userId, string? entityType, string? auditAction, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (!string.IsNullOrEmpty(auditAction))
            {
                query = query.Where(a => a.Action == auditAction);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value.AddDays(1).AddSeconds(-1));
            }

            var logs = await query.OrderByDescending(a => a.Timestamp).Take(500).ToListAsync();

            var users = await _context.Users.Select(u => new { u.Id, u.FullName }).ToListAsync();
            var entityTypes = await _context.AuditLogs.Select(a => a.EntityType).Distinct().ToListAsync();
            var actions = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();

            var viewModel = new AuditLogIndexViewModel
            {
                AuditLogs = logs,
                UserId = userId,
                EntityType = entityType,
                AuditAction = auditAction,
                StartDate = startDate,
                EndDate = endDate,
                Users = new SelectList(users, "Id", "FullName", userId),
                EntityTypes = new SelectList(entityTypes, entityType),
                Actions = new SelectList(actions, auditAction)
            };

            return View(viewModel);
        }
    }
}

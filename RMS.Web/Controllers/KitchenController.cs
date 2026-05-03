using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Hubs;
using RMS.Web.Models;
using RMS.Web.Services;

namespace RMS.Web.Controllers
{
    [Authorize]
    public class KitchenController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IBranchService _branchService;

        public KitchenController(ApplicationDbContext context, IHubContext<OrderHub> hubContext, IBranchService branchService)
        {
            _context = context;
            _hubContext = hubContext;
            _branchService = branchService;
        }

        [Authorize(Policy = Permissions.Orders.UpdateStatus)]
        public async Task<IActionResult> Index()
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing)
                .AsQueryable();

            query = await _branchService.FilterOrdersByBranchAsync(User, query);

            var orders = await query.OrderBy(o => o.CreatedAt).ToListAsync();
            return View(orders);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.UpdateStatus)]
        public async Task<IActionResult> StartPreparing(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            order.Status = OrderStatus.Preparing;
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", id, "Preparing");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.UpdateStatus)]
        public async Task<IActionResult> MarkReady(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            order.Status = order.OrderType == OrderType.Delivery ? OrderStatus.Ready : OrderStatus.ReadyForPickup;
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", id, order.Status.ToString());

            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Hubs;
using RMS.Web.Models;
using RMS.Web.Services;
using System.Security.Claims;

namespace RMS.Web.Controllers
{
    [Authorize]
    public class DeliveryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IBranchService _branchService;

        public DeliveryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<OrderHub> hubContext, IBranchService branchService)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _branchService = branchService;
        }

        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.DeliveryAddress)
                .Where(o => (o.DeliveryUserId == userId && o.Status == OrderStatus.OutForDelivery) || 
                            (o.Status == OrderStatus.Ready && o.OrderType == OrderType.Delivery))
                .AsQueryable();

            query = await _branchService.FilterOrdersByBranchAsync(User, query);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.AssignDelivery)]
        public async Task<IActionResult> PickUp(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FindAsync(id);
            
            if (order == null || order.Status != OrderStatus.Ready || order.OrderType != OrderType.Delivery)
            {
                return BadRequest("Order is not available for pickup.");
            }

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            order.DeliveryUserId = userId;
            order.Status = OrderStatus.OutForDelivery;
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", id, "OutForDelivery");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.UpdateStatus)]
        public async Task<IActionResult> MarkDelivered(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FindAsync(id);

            if (order == null || order.DeliveryUserId != userId || order.Status != OrderStatus.OutForDelivery)
            {
                return BadRequest("Order cannot be marked as delivered.");
            }

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            order.Status = OrderStatus.Delivered;
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", id, "Delivered");

            return RedirectToAction(nameof(Index));
        }
    }
}

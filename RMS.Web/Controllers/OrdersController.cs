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

namespace RMS.Web.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBranchService _branchService;

        public OrdersController(ApplicationDbContext context, IHubContext<OrderHub> hubContext, UserManager<ApplicationUser> userManager, IBranchService branchService)
        {
            _context = context;
            _hubContext = hubContext;
            _userManager = userManager;
            _branchService = branchService;
        }

        // GET: Orders
        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Index()
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .AsQueryable();

            query = await _branchService.FilterOrdersByBranchAsync(User, query);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(orders);
        }

        // GET: Orders/Details/5
        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c.Addresses)
                .Include(o => o.Branch)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.DeliveryAddress)
                .Include(o => o.CreatedByUser)
                .Include(o => o.DeliveryUser)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            ViewBag.Timeline = await _context.AuditLogs
                .Where(l => l.EntityType == "Order" && l.EntityId == id.ToString())
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            if (order.OrderType == OrderType.Delivery)
            {
                ViewBag.DeliveryAgents = await _userManager.GetUsersInRoleAsync("Delivery");
            }

            return View(order);
        }

        // GET: Orders/Receipt/5
        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Receipt(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.DeliveryAddress)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            return View(order);
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Orders.UpdateStatus)]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            // Notify via SignalR
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", id, status.ToString());

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Orders/CancelOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Orders.Cancel)]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            // Notify via SignalR
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", id, OrderStatus.Cancelled.ToString());

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Orders/AssignDelivery
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Orders.AssignDelivery)]
        public async Task<IActionResult> AssignDelivery(int id, string deliveryUserId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null || order.OrderType != OrderType.Delivery)
            {
                return NotFound();
            }

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            order.DeliveryUserId = deliveryUserId;
            // Transition status to OutForDelivery if it was Ready
            if (order.Status == OrderStatus.Ready)
            {
                order.Status = OrderStatus.OutForDelivery;
            }
            
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", id, order.Status.ToString());

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Orders/EditItems/5
        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> EditItems(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            {
                TempData["Error"] = "Only pending or confirmed orders can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Categories = await _context.Categories
                .Include(c => c.MenuItems)
                .ToListAsync();

            return View(order);
        }

        // POST: Orders/EditItems/5
        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> EditItems(int id, [FromBody] List<OrderItemRequest> items)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.ID == id);

            if (order == null) return NotFound();

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            {
                return BadRequest("Order cannot be edited in its current status.");
            }

            // Remove existing items
            _context.OrderItems.RemoveRange(order.OrderItems);

            // Add new items
            foreach (var item in items)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = id,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    Notes = item.Notes
                });
            }

            // Recalculate TotalPrice
            order.TotalPrice = items.Sum(i => i.Price * i.Quantity) + order.DeliveryFee - order.DiscountAmount;

            await _context.SaveChangesAsync();

            // Notify via SignalR
            await _hubContext.Clients.All.SendAsync("OrderUpdated", id);

            return Json(new { success = true });
        }

        // POST: Orders/ChangeType
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> ChangeType(int id, OrderType newType, int? deliveryAddressId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Branch)
                    .ThenInclude(b => b.BranchDeliveryZones)
                .FirstOrDefaultAsync(o => o.ID == id);

            if (order == null) return NotFound();

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            if (order.OrderType == newType)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            if (newType == OrderType.Pickup)
            {
                order.OrderType = OrderType.Pickup;
                order.DeliveryAddressId = null;
                order.DeliveryFee = 0;
                order.DeliveryUserId = null;
            }
            else // Pickup -> Delivery
            {
                if (deliveryAddressId == null)
                {
                    TempData["Error"] = "Delivery address is required for delivery orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var address = await _context.CustomerAddresses.FindAsync(deliveryAddressId);
                if (address == null)
                {
                    TempData["Error"] = "Invalid delivery address.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Find delivery fee for this zone in this branch
                var zone = order.Branch.BranchDeliveryZones
                    .FirstOrDefault(z => z.ZoneName.Equals(address.Zone, StringComparison.OrdinalIgnoreCase));

                decimal deliveryFee = 0;
                if (zone != null)
                {
                    deliveryFee = zone.DeliveryFee;
                }
                else
                {
                    // Fallback to system setting if no zone match
                    var feeSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DeliveryFee");
                    if (feeSetting != null && decimal.TryParse(feeSetting.Value, out decimal fee))
                    {
                        deliveryFee = fee;
                    }
                    else
                    {
                        deliveryFee = 5.00m; // Default
                    }
                }

                order.OrderType = OrderType.Delivery;
                order.DeliveryAddressId = deliveryAddressId;
                order.DeliveryFee = deliveryFee;
            }

            // Recalculate TotalPrice
            // TotalPrice = Sum(items) + DeliveryFee - DiscountAmount
            var subtotal = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
            order.TotalPrice = subtotal + order.DeliveryFee - order.DiscountAmount;

            await _context.SaveChangesAsync();

            // Notify via SignalR
            await _hubContext.Clients.All.SendAsync("OrderUpdated", id);

            TempData["Success"] = $"Order type changed to {newType}.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

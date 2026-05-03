using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Helpers;
using RMS.Web.Hubs;
using RMS.Web.Models;
using RMS.Web.Services;
using System.Security.Claims;

namespace RMS.Web.Controllers
{
    [Authorize]
    public class CallCenterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IBranchService _branchService;

        public CallCenterController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<OrderHub> hubContext, IBranchService branchService)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _branchService = branchService;
        }

        [Authorize(Policy = Permissions.Orders.Create)]
        public async Task<IActionResult> Index()
        {
            var branchIds = await _branchService.GetUserBranchIdsAsync(User);
            ViewBag.Branches = await _context.Branches
                .Where(b => branchIds.Contains(b.ID))
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .Include(c => c.MenuItems)
                    .ThenInclude(m => m.Extras)
                .Include(c => c.MenuItems)
                    .ThenInclude(m => m.Variants)
                .ToListAsync();

            var deliveryFeeSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DeliveryFee");
            ViewBag.DeliveryFee = deliveryFeeSetting?.Value ?? "5.00";

            return View();
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Orders.Create)]
        public async Task<IActionResult> GetEligibleBranch(int addressId)
        {
            var address = await _context.CustomerAddresses.FindAsync(addressId);
            if (address == null || string.IsNullOrEmpty(address.Zone))
            {
                return NotFound("Address or Zone not found.");
            }

            var branchIds = await _branchService.GetUserBranchIdsAsync(User);

            var branches = await _context.Branches
                .Include(b => b.BranchDeliveryZones)
                .Where(b => b.IsOpen && b.BranchDeliveryZones.Any(z => z.ZoneName == address.Zone) && branchIds.Contains(b.ID))
                .ToListAsync();

            // Filter by working hours using helper
            var eligibleBranch = branches.FirstOrDefault(b => BranchHelper.IsWithinWorkingHours(b.WorkingHours));

            if (eligibleBranch == null)
            {
                return NotFound("No open branch found for this zone at the current time among your assigned branches.");
            }

            return Json(new { id = eligibleBranch.ID, name = eligibleBranch.Name });
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Orders.Create)]
        public async Task<IActionResult> SearchCustomer(string phone)
        {
            var customer = await _context.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Phone == phone);
            
            if (customer == null) return NotFound();
            
            return Json(customer);
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> GetCustomerHistory(int customerId)
        {
            var branchIds = await _branchService.GetUserBranchIdsAsync(User);

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId && branchIds.Contains(o.BranchId))
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new {
                    o.ID,
                    o.CreatedAt,
                    o.TotalPrice,
                    o.OrderType,
                    Items = o.OrderItems.Select(oi => new { oi.MenuItem.Name, oi.Quantity })
                })
                .ToListAsync();
            
            return Json(orders);
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> GetOrderItems(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (!await _branchService.CanAccessBranchAsync(User, order.BranchId))
            {
                return Forbid();
            }

            var items = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => new {
                    oi.MenuItemId,
                    oi.MenuItem.Name,
                    Price = oi.UnitPrice,
                    oi.Quantity,
                    oi.Notes
                })
                .ToListAsync();

            return Json(items);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Create)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return BadRequest("Invalid order request.");
            }

            // 1. Validate Branch Access & Status
            if (!await _branchService.CanAccessBranchAsync(User, request.BranchId))
            {
                return Forbid();
            }

            var branch = await _context.Branches.FindAsync(request.BranchId);
            if (branch == null || !branch.IsOpen)
            {
                return BadRequest("The selected branch is currently closed.");
            }

            // Validate Working Hours
            if (!BranchHelper.IsWithinWorkingHours(branch.WorkingHours))
            {
                return BadRequest($"The branch is currently closed or outside its working hours ({branch.WorkingHours}).");
            }

            // 2. Validate Item Availability (Global and Branch-specific)
            foreach (var itemRequest in request.Items)
            {
                var menuItem = await _context.MenuItems
                    .Include(m => m.BranchAvailabilities)
                    .FirstOrDefaultAsync(m => m.ID == itemRequest.MenuItemId);

                if (menuItem == null || !menuItem.IsAvailableGlobal)
                {
                    return BadRequest($"Item '{itemRequest.Name}' is no longer available.");
                }

                // Check Scheduled Availability
                var localNow = DateTime.Now;

                if (menuItem.AvailableFrom.HasValue && localNow < menuItem.AvailableFrom.Value)
                {
                    return BadRequest($"Item '{itemRequest.Name}' is not yet available. It will be available from {menuItem.AvailableFrom.Value:yyyy-MM-dd HH:mm}.");
                }

                if (menuItem.AvailableTo.HasValue && localNow > menuItem.AvailableTo.Value)
                {
                    return BadRequest($"Item '{itemRequest.Name}' is no longer available. It was available until {menuItem.AvailableTo.Value:yyyy-MM-dd HH:mm}.");
                }

                var branchAvailability = menuItem.BranchAvailabilities
                    .FirstOrDefault(a => a.BranchId == request.BranchId);
                
                if (branchAvailability != null && !branchAvailability.IsAvailable)
                {
                    return BadRequest($"Item '{itemRequest.Name}' is not available in the selected branch.");
                }
            }

            // Get current user ID
            var userId = _userManager.GetUserId(User) ?? "system";
            
            var deliveryFeeSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DeliveryFee");
            decimal deliveryFee = 5.00m;
            if (deliveryFeeSetting != null && decimal.TryParse(deliveryFeeSetting.Value, out decimal fee))
            {
                deliveryFee = fee;
            }

            var order = new Order
            {
                CustomerId = request.CustomerId,
                BranchId = request.BranchId,
                OrderType = request.OrderType,
                TotalPrice = request.Items.Sum(i => i.Price * i.Quantity) + (request.OrderType == OrderType.Delivery ? deliveryFee : 0) - request.DiscountAmount,
                DiscountAmount = request.DiscountAmount,
                Status = OrderStatus.Pending,
                Notes = request.Notes,
                CreatedByUserId = userId,
                DeliveryAddressId = request.DeliveryAddressId,
                DeliveryFee = request.OrderType == OrderType.Delivery ? deliveryFee : 0,
                CreatedAt = DateTime.UtcNow,
                OrderItems = request.Items.Select(i => new OrderItem
                {
                    MenuItemId = i.MenuItemId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Price,
                    Notes = i.Notes
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Notify via SignalR
            await _hubContext.Clients.All.SendAsync("NewOrderReceived", order.ID);

            return Json(new { success = true, orderId = order.ID });
        }
    }

    public class OrderCreateRequest
    {
        public int CustomerId { get; set; }
        public int BranchId { get; set; }
        public OrderType OrderType { get; set; }
        public int? DeliveryAddressId { get; set; }
        public string? Notes { get; set; }
        public decimal DiscountAmount { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }
}

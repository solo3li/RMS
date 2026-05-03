using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Models;
using RMS.Web.Services;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Web.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBranchService _branchService;

        public ReportsController(ApplicationDbContext context, IBranchService branchService)
        {
            _context = context;
            _branchService = branchService;
        }

        [Authorize(Policy = Permissions.Reports.View)]
        public async Task<IActionResult> ExportToCsv()
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .AsQueryable();

            query = await _branchService.FilterOrdersByBranchAsync(User, query);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Order ID,Date,Customer,Branch,Type,Total Price,Status");

            foreach (var order in orders)
            {
                var row = string.Format("{0},{1:yyyy-MM-dd HH:mm},\"{2}\",\"{3}\",{4},{5},{6}",
                    order.ID,
                    order.CreatedAt,
                    order.Customer.Name.Replace("\"", "\"\""),
                    order.Branch.Name.Replace("\"", "\"\""),
                    order.OrderType,
                    order.TotalPrice,
                    order.Status);
                builder.AppendLine(row);
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "OrdersReport.csv");
        }

        [Authorize(Policy = Permissions.Reports.View)]
        public async Task<IActionResult> Index()
        {
            var query = _context.Orders
                .Include(o => o.Branch)
                .Include(o => o.CreatedByUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.Status != OrderStatus.Cancelled)
                .AsQueryable();

            query = await _branchService.FilterOrdersByBranchAsync(User, query);

            var orders = await query.ToListAsync();

            var viewModel = new ReportsViewModel
            {
                TotalRevenue = orders.Sum(o => o.TotalPrice),
                TotalOrders = orders.Count,
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalPrice) : 0,

                OrderTypeBreakdowns = orders.GroupBy(o => o.OrderType)
                    .Select(g => new OrderTypeBreakdown
                    {
                        OrderType = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.TotalPrice)
                    }).ToList(),

                TopMenuItems = orders.SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.MenuItem.Name)
                    .Select(g => new TopMenuItem
                    {
                        MenuItemName = g.Key,
                        Quantity = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(10)
                    .ToList(),

                BranchRevenues = orders.GroupBy(o => o.Branch.Name)
                    .Select(g => new BranchRevenue
                    {
                        BranchName = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalPrice)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToList(),

                DailyRevenues = orders.GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new DailyRevenue
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalPrice)
                    })
                    .OrderBy(x => x.Date)
                    .ToList(),

                StaffPerformances = orders.GroupBy(o => o.CreatedByUser?.UserName ?? "Unknown")
                    .Select(g => new StaffPerformanceViewModel
                    {
                        StaffName = g.Key,
                        TotalOrders = g.Count(),
                        TotalRevenue = g.Sum(o => o.TotalPrice)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToList()
            };

            return View(viewModel);
        }
    }
}

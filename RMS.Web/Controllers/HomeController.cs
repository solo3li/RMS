using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Data;
using RMS.Web.Models;
using RMS.Web.Services;

namespace RMS.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBranchService _branchService;

    public HomeController(ApplicationDbContext context, IBranchService branchService)
    {
        _context = context;
        _branchService = branchService;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        
        var ordersQuery = _context.Orders.AsQueryable();
        ordersQuery = await _branchService.FilterOrdersByBranchAsync(User, ordersQuery);

        var branchesQuery = _context.Branches.AsQueryable();
        var userBranchIds = await _branchService.GetUserBranchIdsAsync(User);
        if (userBranchIds != null)
        {
            branchesQuery = branchesQuery.Where(b => userBranchIds.Contains(b.ID));
        }

        var dashboard = new DashboardViewModel
        {
            TotalOrdersToday = await ordersQuery.CountAsync(o => o.CreatedAt >= today),
            TotalRevenueToday = await ordersQuery.Where(o => o.CreatedAt >= today && o.Status != OrderStatus.Cancelled).SumAsync(o => o.TotalPrice),
            PendingOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Pending),
            ActiveBranches = await branchesQuery.CountAsync(b => b.IsOpen),
            RecentOrders = await ordersQuery
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(dashboard);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

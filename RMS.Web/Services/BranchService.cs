using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Data;
using RMS.Web.Models;
using System.Security.Claims;

namespace RMS.Web.Services
{
    public interface IBranchService
    {
        Task<List<int>> GetUserBranchIdsAsync(ClaimsPrincipal user);
        Task<bool> CanAccessBranchAsync(ClaimsPrincipal user, int branchId);
        Task<IQueryable<Order>> FilterOrdersByBranchAsync(ClaimsPrincipal user, IQueryable<Order> query);
    }

    public class BranchService : IBranchService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public BranchService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<List<int>> GetUserBranchIdsAsync(ClaimsPrincipal user)
        {
            if (user.IsInRole("Admin") || user.IsInRole("Call Center"))
            {
                return await _context.Branches.Select(b => b.ID).ToListAsync();
            }

            var appUser = await _userManager.Users
                .Include(u => u.AssignedBranches)
                .FirstOrDefaultAsync(u => u.UserName == (user.Identity != null ? user.Identity.Name : null));

            if (appUser == null) return new List<int>();

            return appUser.AssignedBranches.Select(b => b.ID).ToList();
        }

        public async Task<bool> CanAccessBranchAsync(ClaimsPrincipal user, int branchId)
        {
            if (user.IsInRole("Admin") || user.IsInRole("Call Center")) return true;

            var branchIds = await GetUserBranchIdsAsync(user);
            return branchIds.Contains(branchId);
        }

        public async Task<IQueryable<Order>> FilterOrdersByBranchAsync(ClaimsPrincipal user, IQueryable<Order> query)
        {
            if (user.IsInRole("Admin") || user.IsInRole("Call Center")) return query;

            var branchIds = await GetUserBranchIdsAsync(user);
            return query.Where(o => branchIds.Contains(o.BranchId));
        }
    }
}

namespace RMS.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalOrdersToday { get; set; }
        public decimal TotalRevenueToday { get; set; }
        public int PendingOrders { get; set; }
        public int ActiveBranches { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }
}

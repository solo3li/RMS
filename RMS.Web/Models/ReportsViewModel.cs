using System.Collections.Generic;

namespace RMS.Web.Models
{
    public class ReportsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<OrderTypeBreakdown> OrderTypeBreakdowns { get; set; } = new();
        public List<TopMenuItem> TopMenuItems { get; set; } = new();
        public List<BranchRevenue> BranchRevenues { get; set; } = new();
        public List<DailyRevenue> DailyRevenues { get; set; } = new();
        public List<StaffPerformanceViewModel> StaffPerformances { get; set; } = new();
    }

    public class StaffPerformanceViewModel
    {
        public string StaffName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class OrderTypeBreakdown
    {
        public OrderType OrderType { get; set; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopMenuItem
    {
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class BranchRevenue
    {
        public string BranchName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}

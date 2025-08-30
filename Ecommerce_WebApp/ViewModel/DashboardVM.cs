namespace Ecommerce_WebApp.ViewModels
{
    public class DashboardVM
    {
        public decimal MonthlyRevenue { get; set; }
        public decimal AnnualRevenue { get; set; }
        public int PendingOrdersCount { get; set; }
        public int TotalOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public int CompletionRate => TotalOrdersCount > 0 ? (int)(((double)CompletedOrdersCount / TotalOrdersCount) * 100) : 0;
        public List<string> MonthlyRevenueLabels { get; set; } // Nhãn các tháng: "Jan", "Feb", ...
        public List<decimal> MonthlyRevenueData { get; set; }
    }
}
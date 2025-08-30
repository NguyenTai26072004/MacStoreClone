using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Utility;
using Ecommerce_WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; 
using System.Globalization; 
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce_WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db; 

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        // Action Index đã được sửa lại để chạy tuần tự

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var completedOrdersQuery = _db.OrderHeaders.Where(oh => oh.OrderStatus == SD.OrderStatusCompleted);

            // --- TÍNH TOÁN DỮ LIỆU CARD THÔNG SỐ (TUẦN TỰ) ---
            // Đợi mỗi câu truy vấn hoàn thành trước khi sang câu tiếp theo
            var monthlyRevenue = await completedOrdersQuery
                .Where(oh => oh.OrderDate.Year == now.Year && oh.OrderDate.Month == now.Month)
                .SumAsync(oh => oh.OrderTotal);

            var annualRevenue = await completedOrdersQuery
                .Where(oh => oh.OrderDate.Year == now.Year)
                .SumAsync(oh => oh.OrderTotal);

            var pendingOrdersCount = await _db.OrderHeaders
                .CountAsync(oh => oh.OrderStatus == SD.OrderStatusPending);

            var totalOrdersCount = await _db.OrderHeaders.CountAsync();
            var completedOrdersCount = await completedOrdersQuery.CountAsync();

            // --- TÍNH TOÁN DỮ LIỆU BIỂU ĐỒ (TUẦN TỰ) ---
            var monthlyRevenueLabels = new List<string>();
            var monthlyRevenueData = new List<decimal>();

            for (int i = 11; i >= 0; i--)
            {
                var monthToCalc = now.AddMonths(-i);
                // "await" nằm BÊN TRONG vòng lặp. Nó sẽ đợi tính xong doanh thu tháng này
                // rồi mới bắt đầu vòng lặp tiếp theo để tính cho tháng sau.
                var revenueOfMonth = await completedOrdersQuery
                                           .Where(oh => oh.OrderDate.Year == monthToCalc.Year && oh.OrderDate.Month == monthToCalc.Month)
                                           .SumAsync(oh => oh.OrderTotal);

                monthlyRevenueLabels.Add(monthToCalc.ToString("MMM", new CultureInfo("vi-VN")));
                monthlyRevenueData.Add(revenueOfMonth);
            }

            // --- TẠO VIEWMODEL ---
            var viewModel = new DashboardVM
            {
                MonthlyRevenue = monthlyRevenue,
                AnnualRevenue = annualRevenue,
                PendingOrdersCount = pendingOrdersCount,
                TotalOrdersCount = totalOrdersCount,
                CompletedOrdersCount = completedOrdersCount,
                MonthlyRevenueLabels = monthlyRevenueLabels,
                MonthlyRevenueData = monthlyRevenueData
            };

            return View(viewModel);
        }
    }
}
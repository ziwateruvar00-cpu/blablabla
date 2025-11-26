using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.ViewModels;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReportController : Controller
    {
        private readonly MynghevietDbContext _db;

        public AdminReportController(MynghevietDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var revenuePoints = await _db.Orders
                .Where(o => o.OrderDate.HasValue)
                .GroupBy(o => o.OrderDate!.Value.Date)
                .Select(g => new RevenuePoint
                {
                    Date = g.Key,
                    Label = g.Key.ToString("dd/MM"),
                    Value = g.Sum(o => o.TotalMoney)
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            var topProducts = await _db.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => od.Product!.Name)
                .Select(g => new TopProductVM
                {
                    Name = g.Key,
                    UnitsSold = g.Sum(x => x.Quantity),
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.PriceAtPurchase)
                })
                .OrderByDescending(tp => tp.UnitsSold)
                .Take(5)
                .ToListAsync();

            var inventoryAlerts = await _db.Products
                .Where(p => p.Stock < 10)
                .Select(p => new InventoryAlertVM
                {
                    ProductName = p.Name,
                    CurrentStock = p.Stock
                })
                .ToListAsync();

            var report = new AdminReportVM
            {
                RevenuePoints = revenuePoints,
                TopProducts = topProducts,
                InventoryAlerts = inventoryAlerts,
                TotalOrders = await _db.Orders.CountAsync(),
                TotalCustomers = await _db.Users.CountAsync(),
                TotalRevenue = await _db.Orders.SumAsync(o => (decimal?)o.TotalMoney) ?? 0m
            };

            ViewData["Title"] = "Thống kê";
            ViewData["Subtitle"] = "Biểu đồ doanh thu và cảnh báo tồn kho";
            return View(report);
        }
    }
}

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Web_ban_do_thu_cong_my_nghe.ViewModels;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly MynghevietDbContext _db;

        public AdminOrderController(MynghevietDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _db.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            var today = DateTime.Today;
            var model = new AdminOrderIndexVM
            {
                Orders = orders,
                PendingCount = orders.Count(o => o.Status == OrderStatusHelper.Pending),
                ShippingCount = orders.Count(o => o.Status == OrderStatusHelper.Shipping),
                CompletedCount = orders.Count(o => o.Status == OrderStatusHelper.Completed),
                InventoryInStock = await _db.Products.SumAsync(p => (int?)p.Stock) ?? 0,
                TotalUnitsSold = await _db.OrderDetails.SumAsync(od => (int?)od.Quantity) ?? 0,
                RevenueToday = orders
                    .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == today)
                    .Sum(o => o.TotalMoney)
            };

            ViewData["Title"] = "Quản lý bán hàng";
            ViewData["Subtitle"] = "Theo dõi đơn hàng, tồn kho và giao hàng";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int status)
        {
            if (!OrderStatusHelper.IsValid(status))
            {
                TempData["StatusMessage"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _db.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã cập nhật trạng thái đơn hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}

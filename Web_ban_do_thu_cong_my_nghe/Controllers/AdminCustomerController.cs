using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Data;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCustomerController : Controller
    {
        private readonly MynghevietDbContext _db;

        public AdminCustomerController(MynghevietDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _db.Users.OrderBy(u => u.Fullname).ToListAsync();
            ViewData["Title"] = "Khách hàng";
            ViewData["Subtitle"] = "Thông tin khách hàng và hoạt động mua hàng";
            return View(customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _db.Users
                .Include(u => u.Orders)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var customer = await _db.Users.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.Status = !customer.Status;
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = customer.Status ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }
    }
}

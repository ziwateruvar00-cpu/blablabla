using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Data;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminStaffController : Controller
    {
        private readonly MynghevietDbContext _db;

        public AdminStaffController(MynghevietDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var staff = await _db.NhanViens.OrderBy(nv => nv.HoTen).ToListAsync();
            ViewData["Title"] = "Nhân viên";
            ViewData["Subtitle"] = "Theo dõi thông tin và phân quyền nhân sự";
            return View(staff);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _db.NhanViens.Add(model);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã thêm nhân viên mới.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var staff = await _db.NhanViens.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien model)
        {
            if (id != model.MaNV)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _db.Update(model);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã cập nhật thông tin nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var staff = await _db.NhanViens.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            _db.NhanViens.Remove(staff);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã xóa nhân viên.";
            return RedirectToAction(nameof(Index));
        }
    }
}

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Data;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProductController : Controller
    {
        private readonly MynghevietDbContext _db;

        public AdminProductController(MynghevietDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.Categories = await _db.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewData["Title"] = "Quản lý sản phẩm";
            ViewData["Subtitle"] = "Danh sách sản phẩm hiện có";
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            await LoadCategories();
            ViewData["Title"] = "Thêm sản phẩm";
            return View(new Product { Stock = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategories();
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            _db.Products.Add(model);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã thêm sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            await LoadCategories();
            ViewData["Title"] = "Chỉnh sửa sản phẩm";
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories();
                return View(model);
            }

            var existing = await _db.Products.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.ImageUrl = model.ImageUrl;
            existing.Stock = model.Stock;
            existing.CategoryId = model.CategoryId;
            existing.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã cập nhật sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã xoá sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCategories()
        {
            ViewBag.Categories = await _db.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}

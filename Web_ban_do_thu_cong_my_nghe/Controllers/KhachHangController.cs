using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Web_ban_do_thu_cong_my_nghe.ViewModels;
using Web_ban_do_thu_cong_my_nghe.ViewModels.Account;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Customer")]
    public class KhachHangController : Controller
    {
        private readonly MynghevietDbContext db;
        private readonly IMapper _mapper;

        public KhachHangController(MynghevietDbContext context, IMapper mapper)
        {
            db = context;
            _mapper = mapper;
        }

        #region Register

        
        [AllowAnonymous]
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [AllowAnonymous] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKy(RegisterVM model)
        {
            if (ModelState.IsValid)
            {

                var existingUser = db.Users.SingleOrDefault(u => u.TenDangNhap == model.TenDangNhap || u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc email đã tồn tại.");
                    return View(model);
                }

                var randomKeyMoi = MyUtil.GenerateRamdomKey();

                var user = new User
                {
                    Fullname = model.HoTen,
                    Email = model.Email,
                    TenDangNhap = model.TenDangNhap,
                    Password = model.MatKhau.ToMd5Hash(randomKeyMoi),
                    RandomKey = randomKeyMoi,
                    Status = true,
                    Role = "Customer", // << TỐT!
                    Gender = model.GioiTinh,
                    NgaySinh = model.NgaySinh,
                    Address = model.DiaChi,
                    PhoneNumber = model.DienThoai,
                    Hinh = model.Hinh,
                    CreatedAt = DateTime.Now
                };

                db.Users.Add(user);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đăng ký thành công, mời đăng nhập!";
                return RedirectToAction("DangNhap", "KhachHang");
            }

            return View(model);
        }

        #endregion


        #region Login in 

        [AllowAnonymous] 
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            ConfigureLoginView("Đăng nhập tài khoản", nameof(DangNhap), nameof(KhachHangController).Replace("Controller", string.Empty), true, ReturnUrl);
            return View(new LoginVM());
        }

        [AllowAnonymous] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            ConfigureLoginView("Đăng nhập tài khoản", nameof(DangNhap), nameof(KhachHangController).Replace("Controller", string.Empty), true, ReturnUrl);
            if (ModelState.IsValid)
            {
                var khachHang = db.Users.SingleOrDefault(kh => kh.TenDangNhap == model.TenDangNhap);

                if (khachHang == null)
                {
                    ModelState.AddModelError("Lỗi", "Tên đăng nhập hoặc mật khẩu không đúng");
                }
                else
                {

                    if (!khachHang.Status)
                    {
                        ModelState.AddModelError("Lỗi", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin");
                    }
                    else
                    {
                        var normalizedRole = NormalizeRole(khachHang.Role);
                        if (!IsPasswordValid(khachHang, model.Password, normalizedRole))
                        {
                            ModelState.AddModelError("Lỗi", "Sai thông tin đăng nhập ");
                        }
                        else
                        {
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, khachHang.TenDangNhap ?? khachHang.Fullname ?? string.Empty),
                                new Claim(MySetting.CLAIM_CUSTOMERID, khachHang.Id.ToString()),
                                new Claim(ClaimTypes.Role, normalizedRole)
                            };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                            await HttpContext.SignInAsync(claimsPrincipal);

                            return RedirectAfterLogin(normalizedRole, ReturnUrl);
                        }
                    }
                }
            }

            return View(model);
        }

        #endregion

        private void ConfigureLoginView(string title, string actionName, string controllerName, bool showForgot, string? returnUrl = null, string? description = null)
        {
            ViewBag.LoginTitle = title;
            ViewBag.LoginDescription = description;
            ViewBag.FormAction = actionName;
            ViewBag.FormController = controllerName;
            ViewBag.ShowForgot = showForgot;
            ViewBag.ReturnUrl = returnUrl;
        }

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return "Customer";
            }

            return role.Trim();
        }

        private static bool IsPasswordValid(User user, string providedPassword, string normalizedRole)
        {
            var hashedInput = providedPassword.ToMd5Hash(user.RandomKey);
            if (string.Equals(user.Password, hashedInput, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(user.Password, providedPassword, StringComparison.Ordinal);
            }

            return false;
        }

        private IActionResult RedirectAfterLogin(string normalizedRole, string? returnUrl)
        {
            if (string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("/");
        }

       
        public async Task<IActionResult> Profile()
        {
            
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);

         
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            
            var customer = await db.Users.FindAsync(int.Parse(customerId));

            if (customer == null)
            {
                return NotFound();
            }

            
            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            var customer = await db.Users.FindAsync(int.Parse(customerId));
            if (customer == null)
            {
                return NotFound();
            }

            var model = new EditProfileVM
            {
                HoTen = customer.Fullname,
                Email = customer.Email,
                SoDienThoai = customer.PhoneNumber,
                DiaChi = customer.Address,
                NgaySinh = customer.NgaySinh,
                GioiTinh = customer.Gender
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileVM model)
        {
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await db.Users.FindAsync(int.Parse(customerId));
            if (customer == null)
            {
                return NotFound();
            }

            var emailExists = await db.Users
                .AnyAsync(u => u.Email == model.Email && u.Id != customer.Id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng bởi tài khoản khác.");
                return View(model);
            }

            customer.Fullname = model.HoTen;
            customer.Email = model.Email;
            customer.PhoneNumber = model.SoDienThoai;
            customer.Address = model.DiaChi;
            customer.NgaySinh = model.NgaySinh;
            customer.Gender = model.GioiTinh;

            db.Users.Update(customer);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            return View(new ChangePasswordVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await db.Users.FindAsync(int.Parse(customerId));
            if (customer == null)
            {
                return NotFound();
            }

            var currentHash = model.MatKhauHienTai.ToMd5Hash(customer.RandomKey);
            if (!string.Equals(currentHash, customer.Password, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            var newRandomKey = MyUtil.GenerateRamdomKey();
            customer.RandomKey = newRandomKey;
            customer.Password = model.MatKhauMoi.ToMd5Hash(newRandomKey);

            db.Users.Update(customer);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> Account(string? tab)
        {
            var customerIdValue = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerIdValue) || !int.TryParse(customerIdValue, out var customerId))
            {
                return RedirectToAction("DangNhap");
            }

            var customer = await db.Users.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }

            var ordersQuery = db.Orders.Where(o => o.UserId == customerId);
            var totalOrders = await ordersQuery.CountAsync();
            var totalSpent = await ordersQuery.SumAsync(o => (decimal?)o.TotalMoney) ?? 0m;

            var favoriteProducts = await db.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order != null && od.Order.UserId == customerId && od.Product != null)
                .GroupBy(od => od.Product!)
                .Select(g => new FavoriteItemVM
                {
                    ProductId = g.Key.Id,
                    Name = g.Key.Name,
                    Price = g.Key.Price,
                    ImageUrl = g.Key.ImageUrl ?? string.Empty,
                    TimesPurchased = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(f => f.TimesPurchased)
                .Take(8)
                .ToListAsync();

            var recentNotifications = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .Select(o => new NotificationVM
                {
                    Title = $"Đơn hàng #{o.Id}",
                    Message = $"Trạng thái hiện tại: {OrderStatusHelper.GetLabel(o.Status)}",
                    CreatedAt = o.OrderDate ?? DateTime.Now,
                    Status = OrderStatusHelper.GetLabel(o.Status),
                    StatusCode = OrderStatusHelper.Normalize(o.Status)
                })
                .ToListAsync();

            var viewModel = new AccountDashboardVM
            {
                User = new AccountUserSummaryVM
                {
                    Id = customer.Id,
                    Fullname = customer.Fullname,
                    Email = customer.Email ?? string.Empty,
                    PhoneNumber = customer.PhoneNumber ?? string.Empty,
                    Address = customer.Address ?? string.Empty,
                    AvatarUrl = customer.Hinh ?? string.Empty,
                    Role = customer.Role ?? string.Empty
                },
                Favorites = favoriteProducts,
                Notifications = recentNotifications,
                Settings = new AccountSettingsVM
                {
                    Fullname = customer.Fullname,
                    Email = customer.Email ?? string.Empty,
                    PhoneNumber = customer.PhoneNumber ?? string.Empty,
                    Address = customer.Address ?? string.Empty,
                    ReceiveEmail = true,
                    ReceiveSms = !string.IsNullOrEmpty(customer.PhoneNumber)
                },
                TotalOrders = totalOrders,
                TotalSpent = totalSpent
            };

            ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab.ToLowerInvariant();
            return View(viewModel);
        }

        [AllowAnonymous]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }


        public async Task<IActionResult> LichSuDonHang()
        {
            var customerId = User.FindFirstValue("CustomerId");
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            var orders = await db.Orders
                .Where(o => o.UserId == int.Parse(customerId))
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(orders);
        }
    }
}






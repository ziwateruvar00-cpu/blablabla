using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Web_ban_do_thu_cong_my_nghe.ViewModels;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    public class CartController : Controller
    {
        private readonly MynghevietDbContext db;

        private const string DefaultShippingKey = "mienphi";
        private const string DefaultPaymentKey = "COD";

        private static readonly Dictionary<string, (string Label, decimal Fee)> ShippingOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "mienphi", ("Miễn phí vận chuyển", 0m) },
            { "tietkiem", ("Giao hàng tiết kiệm", 15000m) },
            { "nhancuahang", ("Nhận tại cửa hàng", 8000m) }
        };

        private static readonly Dictionary<string, string> PaymentOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "COD", "Thanh toán khi nhận hàng" },
            { "BANK", "Chuyển khoản ngân hàng" }
        };

        public CartController(MynghevietDbContext context)
        {
            db = context;
        }

        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
        public IActionResult Index()
        {
            var cartItems = Cart;
            var totals = CalculateCartTotals(cartItems);

            ViewBag.CartSubtotal = totals.Subtotal;
            ViewBag.ShippingFee = totals.ShippingFee;
            ViewBag.DiscountAmount = totals.Discount;
            ViewBag.CartTotal = totals.Total;

            return View(cartItems);
        }
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            if (item == null)
            {
                var hanghoa = db.Products.SingleOrDefault(p => p.Id == id);
                if (hanghoa == null)
                {
                    TempData["Message"] = $"Không tìm thấy hàng hóa có mã {id}";
                    return Redirect("/404");
                }
                item = new CartItem
                {
                    MaHh = hanghoa.Id,
                    TenHH = hanghoa.Name,
                    DonGia = hanghoa.Price,
                    Hinh = hanghoa.ImageUrl ?? string.Empty,
                    SoLuong = quantity
                };
                gioHang.Add(item);
            }
            else
            {
                item.SoLuong += quantity;
            }
            HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            return RedirectToAction("Index");
        }
        public IActionResult RemoveCart(int id)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            if (item != null)
            {
                gioHang.Remove(item);
                HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            if (quantity < 1)
            {
                quantity = 1;
            }

            var cartItems = Cart;
            var item = cartItems.SingleOrDefault(p => p.MaHh == id);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
            }

            item.SoLuong = quantity;
            HttpContext.Session.Set(MySetting.CART_KEY, cartItems);

            var totals = CalculateCartTotals(cartItems);

            return Json(new
            {
                success = true,
                quantity = item.SoLuong,
                lineTotal = item.ThanhTien,
                subtotal = totals.Subtotal,
                shippingFee = totals.ShippingFee,
                discount = totals.Discount,
                total = totals.Total
            });
        }
        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            var cartItems = Cart;

            PrepareCheckoutViewData();

            if (cartItems.Count == 0)
            {
                ViewBag.EmptyMessage = "Giỏ hàng của bạn đang trống.";
            }

            return View(cartItems);
        }
        [Authorize] 
        [HttpPost]
        public IActionResult Checkout(CheckoutVM model)
        {
            var cartItems = Cart;
            if (cartItems.Count == 0)
            {
                ViewBag.EmptyMessage = "Giỏ hàng của bạn đang trống.";
                PrepareCheckoutViewData(model.PhuongThucVanChuyen, model.PhuongThucThanhToan);
                return View(cartItems);
            }

            var selectedShipping = NormalizeShippingKey(model.PhuongThucVanChuyen);
            var selectedPayment = NormalizePaymentKey(model.PhuongThucThanhToan);

            PrepareCheckoutViewData(selectedShipping, selectedPayment);

            var shippingInfo = ShippingOptions[selectedShipping];
            var paymentLabel = PaymentOptions[selectedPayment];
            model.PhiVanChuyen = shippingInfo.Fee;
            model.PhuongThucVanChuyen = selectedShipping;
            model.PhuongThucThanhToan = selectedPayment;

            if (!ModelState.IsValid)
            {
                return View(cartItems); 
            }

           
            var customerIdClaim = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID);
            if (customerIdClaim == null)
            {
                return RedirectToAction("DangNhap", "KhachHang", new { ReturnUrl = "/Cart/Checkout" });
            }
            int customerId = int.Parse(customerIdClaim.Value);

            var khachHang = db.Users.SingleOrDefault(kh => kh.Id == customerId);
            if (khachHang == null)
            {
                return RedirectToAction("DangNhap", "KhachHang"); 
            }

            var shippingAddress = string.IsNullOrWhiteSpace(model.DiaChi)
                ? khachHang.Address
                : model.DiaChi;
            var shippingPhone = string.IsNullOrWhiteSpace(model.SoDienThoai)
                ? khachHang.PhoneNumber
                : model.SoDienThoai;

            if (string.IsNullOrWhiteSpace(shippingAddress) || string.IsNullOrWhiteSpace(shippingPhone))
            {
                ModelState.AddModelError("", "Vui lòng cung cấp địa chỉ và số điện thoại nhận hàng.");
                return View(cartItems);
            }
            var orderSubtotal = cartItems.Sum(item => item.ThanhTien);
            var orderTotal = orderSubtotal + shippingInfo.Fee;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = new Order
                    {
                        UserId = customerId,
                        OrderDate = DateTime.Now,
                        Status = OrderStatusHelper.Pending,
                        TotalMoney = orderTotal,
                        Notes = BuildOrderNotes(model.Ghichu, paymentLabel, shippingInfo.Label),
                        ShippingAddress = shippingAddress!,
                        ShippingPhone = shippingPhone!
                    };

                    db.Orders.Add(order);
                    db.SaveChanges();

                    var orderDetails = cartItems.Select(item => new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.MaHh,
                        Quantity = item.SoLuong,
                        PriceAtPurchase = item.DonGia
                    }).ToList();

                    db.OrderDetails.AddRange(orderDetails);
                    db.SaveChanges();

                    transaction.Commit();
                    HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
                    return View("Success");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Đã có lỗi xảy ra khi đặt hàng: " + inner);
                    return View(cartItems);
                }
            }
        }

        private void PrepareCheckoutViewData(string? shippingKey = null, string? paymentKey = null)
        {
            var normalizedShipping = NormalizeShippingKey(shippingKey);
            var normalizedPayment = NormalizePaymentKey(paymentKey);

            ViewBag.ShippingOptions = ShippingOptions
                .Select(kvp => new { Key = kvp.Key, Label = kvp.Value.Label, Fee = kvp.Value.Fee })
                .ToList();

            ViewBag.PaymentOptions = PaymentOptions
                .Select(kvp => new { Key = kvp.Key, Label = kvp.Value })
                .ToList();

            ViewBag.SelectedShipping = normalizedShipping;
            ViewBag.SelectedPayment = normalizedPayment;
            ViewBag.ShippingFee = ShippingOptions[normalizedShipping].Fee;
        }

        private static string NormalizeShippingKey(string? shippingKey)
        {
            if (!string.IsNullOrWhiteSpace(shippingKey) && ShippingOptions.ContainsKey(shippingKey))
            {
                return shippingKey;
            }

            return DefaultShippingKey;
        }

        private static string NormalizePaymentKey(string? paymentKey)
        {
            if (!string.IsNullOrWhiteSpace(paymentKey) && PaymentOptions.ContainsKey(paymentKey))
            {
                return paymentKey;
            }

            return DefaultPaymentKey;
        }

        private static string? BuildOrderNotes(string? originalNote, string paymentLabel, string shippingLabel)
        {
            var noteBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(originalNote))
            {
                noteBuilder.AppendLine(originalNote.Trim());
            }

            noteBuilder.AppendLine($"Thanh toán: {paymentLabel}");
            noteBuilder.Append($"Vận chuyển: {shippingLabel}");

            return noteBuilder.ToString();
        }

        private CartTotals CalculateCartTotals(List<CartItem> cartItems)
        {
            var subtotal = cartItems.Sum(item => item.ThanhTien);
            var shippingFee = ShippingOptions.TryGetValue(DefaultShippingKey, out var defaultShipping)
                ? defaultShipping.Fee
                : 0m;
            const decimal discount = 0m;
            var total = subtotal + shippingFee - discount;
            return new CartTotals(subtotal, shippingFee, discount, total);
        }

        private sealed record CartTotals(decimal Subtotal, decimal ShippingFee, decimal Discount, decimal Total);
    }
}
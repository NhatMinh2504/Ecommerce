using EcommerceMVC.Data;
using EcommerceMVC.Helpers;
using EcommerceMVC.Services;
using EcommerceMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly Hshop2023Context _db;
        private readonly IVnPayService _vnPayService;

        public CartController(Hshop2023Context context,IVnPayService vnPayService)
        {
            _db = context;
            _vnPayService= vnPayService;
        }
        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();

        // Helper: lấy giỏ từ Session
        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY);
            return cart ?? new List<CartItem>();
        }

        // Helper: lưu giỏ vào Session
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.Set(MySetting.CART_KEY, cart);
        }

        public IActionResult Index()
        {
            return View(GetCart());
        }

        // Thêm vào giỏ
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.MaHH == id);

            if (item == null)
            {
                var hanghoa = _db.HangHoas.AsNoTracking().FirstOrDefault(p => p.MaHh == id);
                if (hanghoa == null)
                {
                    TempData["Message"] = "Sản phẩm không tồn tại";
                    return Redirect("/404");
                }

                item = new CartItem
                {
                    MaHH = hanghoa.MaHh,
                    Hinh = hanghoa.Hinh ?? "noimage.jpg",
                    TenHH = hanghoa.TenHh,
                    DonGia = hanghoa.DonGia ?? 0,
                    SoLuong = quantity
                };
                cart.Add(item);
            }
            else
            {
                checked
                {
                    item.SoLuong += quantity;
                    if (item.SoLuong < 1) item.SoLuong = 1;
                }
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // Xoá 1 sản phẩm khỏi giỏ
        public IActionResult RemoveCart(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.MaHH == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // ===== API cập nhật số lượng (gọi từ AJAX) =====
        // Nếu bạn dùng fetch('/Cart/UpdateQty', { method:'POST', body: JSON.stringify({ id, qty }) ... })
        // nhớ bỏ comment trong hàm persistQty ở view.
        public class UpdateQtyRequest
        {
            public int Id { get; set; }
            public int Qty { get; set; }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // đơn giản để gọi từ JS; nếu muốn bảo mật hơn, dùng AntiForgeryToken
        public IActionResult UpdateQty([FromBody] UpdateQtyRequest req)
        {
            if (req == null || req.Id <= 0)
                return BadRequest(new { ok = false, message = "Dữ liệu không hợp lệ." });

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.MaHH == req.Id);
            if (item == null)
                return NotFound(new { ok = false, message = "Không tìm thấy sản phẩm trong giỏ." });

            var qty = req.Qty < 1 ? 1 : req.Qty;
            item.SoLuong = qty;

            // Tính lại line total & totals
            double lineTotal = item.DonGia * item.SoLuong;
            double subtotal = cart.Sum(p => p.DonGia * p.SoLuong);
            const double shipping = 3.0; // nếu bạn có logic ship khác thì chỉnh ở đây
            double total = subtotal + shipping;

            SaveCart(cart);

            return Ok(new
            {
                ok = true,
                id = item.MaHH,
                qty = item.SoLuong,
                lineTotal,
                subtotal,
                shipping,
                total
            });
        }

        // (Tuỳ chọn) Xoá toàn bộ giỏ
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Clear()
        {
            SaveCart(new List<CartItem>());
            return RedirectToAction("Index");
        }
        // [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["Message"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index");
            }
            return View(cart);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutVM model, string payment = PaymentType.COD)
        {
            if (!ModelState.IsValid)
                return View(GetCart()); // hoặc return View(model) tùy bạn

            // Nếu bấm nút VNPAY -> tạo URL và Redirect sang VNPAY
            if (payment == PaymentType.VNPAY)
            {
                var vnReq = new VnPaymentRequestModel
                {
                    Amount = Cart.Sum(p => p.ThanhTien),   // chú ý: service VNPAY thường yêu cầu x100
                    CreatedDate = DateTime.Now,
                    OrderDescription = $"{model.HoTen} {model.DienThoai}",
                    FullName = model.HoTen,
                    OrderId = new Random().Next(100000, 999999)
                };
                return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnReq));
            }

            // ====== COD flow ======
            var customerId = HttpContext.User?.Claims
                .SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            var khachHang = new KhachHang();
            if (model.GiongKhachHang && !string.IsNullOrEmpty(customerId))
            {
                khachHang = _db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
            }

            var hoadon = new HoaDon
            {
                MaKh = customerId,
                HoTen = model.HoTen ?? khachHang?.HoTen,
                DiaChi = model.DiaChi ?? khachHang?.DiaChi,
                DienThoai = model.DienThoai ?? khachHang?.DienThoai,
                NgayDat = DateTime.Now,
                CachThanhToan = PaymentType.COD,
                CachVanChuyen = "Grap",
                MaTrangThai = 0,
                GhiChu = model.GhiChu
            };

            using (var tran = _db.Database.BeginTransaction())
            {
                try
                {
                    _db.HoaDons.Add(hoadon);
                    _db.SaveChanges();

                    // Thêm chi tiết hoá đơn từ giỏ
                    var details = Cart.Select(x => new ChiTietHd
                    {
                        MaHd = hoadon.MaHd,
                        MaHh = x.MaHH,
                        SoLuong = x.SoLuong,
                        DonGia = x.DonGia
                    }).ToList();

                    _db.ChiTietHds.AddRange(details);
                    _db.SaveChanges();

                    tran.Commit();

                    // Xoá giỏ
                    SaveCart(new List<CartItem>());
                    return RedirectToAction("PaymentSuccess");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    ModelState.AddModelError("", "Lỗi khi lưu hoá đơn: " + ex.Message);
                    return View(GetCart());
                }
            }
        }
        public IActionResult PaymentSuccess()
        {
            return View("Success");
        }
        public IActionResult PaymentFail()
        {
            return View();
        }
        //   [Authorize]
        [AllowAnonymous]
        public IActionResult PaymentCallBack()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Lỗi thanh toán VN Pay: {response.VnPayResponseCode}";
                return RedirectToAction("PaymentFail");
            }


            // Lưu đơn hàng vô database

            TempData["Message"] = $"Thanh toán VNPay thành công";
            return RedirectToAction("PaymentSuccess");
        }
    }
}

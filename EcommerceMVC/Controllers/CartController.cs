using EcommerceMVC.Data;
using EcommerceMVC.Helpers;
using EcommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly Hshop2023Context _db;

        public CartController(Hshop2023Context context)
        {
            _db = context;
        }

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
    }
}

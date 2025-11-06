using EcommerceMVC.Data;
using EcommerceMVC.Helpers;
using EcommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly Hshop2023Context db;
        public CartController(Hshop2023Context context)
        {
            db = context;
        }
        const string CART_KEY = "MYCART";
        public List<CartItem> Cart()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("MYCART");
            if (cart == null)
            {
                cart = new List<CartItem>();
            }
            return cart;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult AddToCart(int id, int quantity=1)
        {
            var giohang=Cart();
            var item = giohang.FirstOrDefault(p => p.MaHH == id);
            if (item == null) { 
                var hanghoa = db.HangHoas.FirstOrDefault(p => p.MaHh == id);
                if(hanghoa==null)
                {
                    TempData["Message"] = "Sản phẩm không tồn tại";
                    return Redirect("/404");
                }
                item=new CartItem
                {
                    MaHH = hanghoa.MaHh,
                    Hinh = hanghoa.Hinh,
                    TenHH = hanghoa.TenHh,
                    DonGia = hanghoa.DonGia ?? 0,
                    SoLuong = quantity
                };
                giohang.Add(item);
                
            }
            else {                 item.SoLuong += quantity;
            }
            HttpContext.Session.Set(CART_KEY, giohang);
            return RedirectToAction("Index");
        }
        public IActionResult RemoveCart(int id)
        {
            var giohang = Cart();
            var item = giohang.FirstOrDefault(p => p.MaHH == id);
            if (item != null)
            {
                giohang.Remove(item);
                HttpContext.Session.Set(CART_KEY, giohang);
            }
            return RedirectToAction("Index");
        }
    }
}

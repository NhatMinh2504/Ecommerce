using EcommerceMVC.Helpers;
using EcommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceMVC.ViewComponents
{
    public class CartViewComponent: ViewComponent
    {
        public IViewComponentResult Invoke()
        {
           var count= HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
            return View("CartPanel",new CartModel
            {
                Quantity= count.Sum(p => p.SoLuong),
                Total= count.Sum(p => p.ThanhTien)
            });
        }
    }
}

using EcommerceMVC.Data;
using EcommerceMVC.Models;
using EcommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EcommerceMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Hshop2023Context _db;

      
        public HomeController(ILogger<HomeController> logger, Hshop2023Context db)
        {
            _logger = logger;
            _db = db;
        }


        public async Task<IActionResult> Index()
        {
          
            try
            {
                _db.Database.SetCommandTimeout(5); 
                var ok = await _db.Database.CanConnectAsync();
                if (!ok) return Content(" Không kết nối được DB. Kiểm tra SQL Server & connection string.");
            }
            catch (Exception ex)
            {
                return Content("❌ Lỗi khi kết nối DB: " + ex.Message);
            }

           
            var products = await _db.HangHoas
                .AsNoTracking()
                .OrderByDescending(h => h.MaHh)
                .Select(h => new HangHoaVM
                {
                    MaHh = h.MaHh,
                    TenHh = h.TenHh,
                    Hinh = h.Hinh ?? "noimage.jpg",
                    DonGia = h.DonGia ?? 0,
                    MoTaNgan = h.MoTa ?? "",
                    TenLoai = "" 
                })
                .Take(12)
                .ToListAsync();

            ViewBag.Categories = await _db.Loais
                .AsNoTracking()
                .OrderBy(l => l.TenLoai)
                .Select(l => new { l.MaLoai, l.TenLoai })
                .ToListAsync();

            return View(products);
        }


        [Route("/404")] public IActionResult PageNotFound() => View();
        public IActionResult Privacy() => View();
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

using EcommerceMVC.Data;
using EcommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EcommerceMVC.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly Hshop2023Context db;
        public HangHoaController(Hshop2023Context context)
        {
            db = context;
        }

        // DUY NHẤT một Index: nhận loai, query, sort
        public IActionResult Index(int? loai, string? query, string? sort)
        {
            var hangHoas = db.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .AsQueryable();

            // Filter theo loại (nếu có)
            if (loai.HasValue)
                hangHoas = hangHoas.Where(hh => hh.MaLoai == loai.Value);

            // Search theo tên (nếu có)
            if (!string.IsNullOrWhiteSpace(query))
            {
                var kw = query.Trim();
                hangHoas = hangHoas.Where(hh => hh.TenHh.Contains(kw));
            }

            // Sort
            switch (sort)
            {
                case "price_asc":
                    // Đưa null xuống cuối rồi tăng dần
                    hangHoas = hangHoas
                        .OrderBy(hh => hh.DonGia == null)
                        .ThenBy(hh => hh.DonGia);
                    break;

                case "price_desc":
                    // Đưa null xuống cuối rồi giảm dần
                    hangHoas = hangHoas
                        .OrderBy(hh => hh.DonGia == null)
                        .ThenByDescending(hh => hh.DonGia);
                    break;

                default:
                    // Mặc định theo tên
                    hangHoas = hangHoas.OrderBy(hh => hh.TenHh);
                    break;
            }

            var result = hangHoas.Select(p => new HangHoaVM
            {
                MaHh = p.MaHh,
                TenHh = p.TenHh,
                Hinh = p.Hinh ?? "",
                DonGia = p.DonGia ?? 0,
                MoTaNgan = p.MoTaDonVi ?? "",
                TenLoai = p.MaLoaiNavigation.TenLoai
            }).ToList();

            // Giữ state cho View
            ViewBag.Loai = loai;
            ViewBag.Query = query;
            ViewBag.Sort = sort;

            return View(result);
        }

        // (Tuỳ chọn) Form Search cũ có thể vẫn gọi "Search" => chuyển hướng về Index
        [HttpGet]
        public IActionResult Search(string? query, string? sort, int? loai)
        {
            return RedirectToAction(nameof(Index), new { query, sort, loai });
        }

        public IActionResult Detail(int id)
        {
            var data = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .SingleOrDefault(p => p.MaHh == id);

            if (data == null)
            {
                TempData["Message"] = $"Không tìm thấy hàng hóa có mã {id}";
                return Redirect("/404");
            }

            var result = new ChiTietHangHoaVM
            {
                MaHh = data.MaHh,
                TenHh = data.TenHh,
                Hinh = data.Hinh ?? "",
                DonGia = data.DonGia ?? 0,
                MoTaNgan = data.MoTaDonVi ?? "",
                TenLoai = data.MaLoaiNavigation.TenLoai,
                ChiTiet = data.MoTa ?? "",
                DiemDanhGia = 5,
                SoLuongTon = 100
            };

            return View(result);
        }
    }
}

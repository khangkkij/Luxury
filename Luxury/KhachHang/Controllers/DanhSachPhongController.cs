using Luxury.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhachSanLuxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DanhSachPhongController : ControllerBase
    {
        private LuxuryHotelContext _context;
        public DanhSachPhongController (LuxuryHotelContext context)
        {
            _context = context;
        }
        private decimal TinhGiaHienTai(LoaiphongBienthe bt)
        {
            var today = DateTime.Now;

            // 1️⃣ Ưu tiên giá lễ
            var giaLe = bt.Thietlapgia
                .FirstOrDefault(g =>
                    g.LoaiGia == "Lễ" &&
                    g.NgayBatDau <= today &&
                    g.NgayKetThuc >= today);

            if (giaLe != null)
                return giaLe.Gia ?? 0;

            // 2️⃣ Nếu là cuối tuần
            if (today.DayOfWeek == DayOfWeek.Saturday ||
                today.DayOfWeek == DayOfWeek.Sunday)
            {
                var giaCuoiTuan = bt.Thietlapgia
                    .FirstOrDefault(g => g.LoaiGia == "Cuối tuần");

                if (giaCuoiTuan != null)
                    return giaCuoiTuan.Gia ?? 0;
            }

            // 3️⃣ Mặc định lấy giá ngày thường
            var giaThuong = bt.Thietlapgia
                .FirstOrDefault(g => g.LoaiGia == "Giá ngày thường");

            return giaThuong?.Gia ?? 0;
        }
        [HttpGet]
        public IActionResult GetAll()
        {
            var ds = _context.LoaiphongBienthes
                .Include(x => x.MaLpNavigation)
                .Include(x => x.Phongs)
                .Include(x => x.Hinhanhs)
                .Include(x => x.Thietlapgia)
                .Include(x => x.ChitietFlashsales)
                    .ThenInclude(x => x.Flashsale)
                .AsEnumerable() // ⚠ bắt buộc
                .Select(x => new
                {
                    x.MaBienThePhong,
                    TenLoai = x.MaLpNavigation.TenLoai,
                    x.TenBienThe,
                    SoNguoiToiDa = x.SoNguoiLon,
                    x.SoNguoiLon,
                    x.SoTreEm,
                    x.DienTich,
                    x.HuongNhin,
                    GiaGoc = TinhGiaHienTai(x),

                    PhanTramGiam = x.ChitietFlashsales
                        .Where(fs =>
                            fs.Flashsale.TrangThai == true &&
                            fs.Flashsale.NgayBatDau <= DateTime.Now &&
                            fs.Flashsale.NgayKetThuc >= DateTime.Now
                        )
                        .Select(fs => fs.PhanTramGiam)
                        .FirstOrDefault(),

                    SoPhongCon = x.Phongs.Count(p => p.TrangThai == 1),

                    AnhDaiDien = x.Hinhanhs
                        .Select(h => h.DuongDan)
                        .FirstOrDefault()
                })
                .ToList();
            var today = DateTime.Now;
            var dsLoaiPhong = _context.Loaiphongs
                .Include(lp => lp.LoaiphongBienthes)
                    .ThenInclude(bt => bt.Thietlapgia)
                .Include(lp => lp.LoaiphongBienthes)
                    .ThenInclude(bt => bt.Hinhanhs)
                .Include(lp => lp.LoaiphongBienthes)
                    .ThenInclude(bt => bt.Phongs)
                        .ThenInclude(p => p.Danhgia)
                .AsEnumerable() // ⚠ Bắt buộc vì dùng method custom
                .Select(lp => new
                {
                    lp.MaLp,
                    lp.TenLoai,
                    lp.MoTa,

                    BienThePhongs = lp.LoaiphongBienthes.Select(bt => new
                    {
                        bt.MaBienThePhong,
                        bt.TenBienThe,
                        bt.DienTich,
                        bt.HuongNhin,

                        Gia = TinhGiaHienTai(bt),

                        HinhAnhs = bt.Hinhanhs
                            .Select(h => h.DuongDan)
                            .FirstOrDefault(),

                        SoLuongDanhGia = bt.Phongs
                            .SelectMany(p => p.Danhgia)
                            .Count(),

                        SoSaoTrungBinh = bt.Phongs
                            .SelectMany(p => p.Danhgia)
                            .Average(d => (double?)d.SoSao) ?? 0
                    }).ToList()
                }).ToList();
            return Ok(new
            {
                DanhSach = ds,
                LoaiPhong = dsLoaiPhong
            });
        }
    }
}

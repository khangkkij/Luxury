using Luxury.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhachSanLuxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChiTietPhongController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public ChiTietPhongController(LuxuryHotelContext context)
        {
            _context = context;
        }
        private static decimal TinhGiaHienTai(LoaiphongBienthe bt)
        {
            var today = DateTime.Now;

            var giaLe = bt.Thietlapgia
                .FirstOrDefault(g =>
                    g.LoaiGia == "Lễ" &&
                    g.NgayBatDau <= today &&
                    g.NgayKetThuc >= today);

            if (giaLe != null)
                return giaLe.Gia ?? 0;

            if (today.DayOfWeek == DayOfWeek.Saturday ||
                today.DayOfWeek == DayOfWeek.Sunday)
            {
                var giaCuoiTuan = bt.Thietlapgia
                    .FirstOrDefault(g => g.LoaiGia == "Cuối tuần");

                if (giaCuoiTuan != null)
                    return giaCuoiTuan.Gia ?? 0;
            }

            var giaThuong = bt.Thietlapgia
                .FirstOrDefault(g => g.LoaiGia == "Giá ngày thường");

            return giaThuong?.Gia ?? 0;
        }
        [HttpGet("{id}")]
        public IActionResult ChiTiet(int id)
        {
            var bienThe = _context.LoaiphongBienthes
                .Include(x => x.Hinhanhs)
                .Include(x => x.MaLpNavigation)
                .Include(x => x.Phongs)
                .Include(x => x.Thietlapgia)
                .Include(x => x.TienichLoaiphongs)
                    .ThenInclude(tlp => tlp.Tienich)
                .Include(x => x.ChitietFlashsales)
                    .ThenInclude(x => x.Flashsale)
                .FirstOrDefault(x => x.MaBienThePhong == id);

            if (bienThe == null)
                return NotFound();
            var bienTheKhac = _context.LoaiphongBienthes
                .Where(bt => bt.MaLp == bienThe.MaLp && bt.MaBienThePhong != id)
                .Include(bt => bt.Hinhanhs)
                .Include(bt => bt.Phongs)
                .Include(bt => bt.Thietlapgia)
                .Include(bt => bt.ChitietFlashsales)
                    .ThenInclude(fs => fs.Flashsale)
                .ToList();
            var current = _context.LoaiphongBienthes
                .Include(x => x.Thietlapgia)
                .FirstOrDefault(x => x.MaBienThePhong == id);

            decimal currentPrice = current.Thietlapgia
                .Where(g => g.LoaiGia == "Giá ngày thường")
                .OrderByDescending(g => g.NgayBatDau)
                .Select(g => (decimal?)g.Gia)
                .FirstOrDefault() ?? 0;
            //var bienTheNgauNhien = _context.LoaiphongBienthes
            //    .Include(bt => bt.MaLpNavigation)
            //    .Include(bt => bt.Hinhanhs)
            //    .Include(bt => bt.Phongs)
            //    .Include(bt => bt.Thietlapgia)
            //    .Include(bt => bt.ChitietFlashsales)
            //        .ThenInclude(fs => fs.Flashsale)
            //    .Where(bt => bt.MaBienThePhong != id)
            //    .OrderBy(x => Guid.NewGuid())
            //    .Take(3)
            //    .ToList();
            var bienTheNgauNhien = _context.LoaiphongBienthes
                .Include(bt => bt.MaLpNavigation)
                .Include(bt => bt.Hinhanhs)
                .Include(bt => bt.Phongs)
                .Include(bt => bt.Thietlapgia)
                .Include(bt => bt.ChitietFlashsales)
                    .ThenInclude(fs => fs.Flashsale)
                .Where(bt => bt.MaBienThePhong != id)
                .Select(bt => new
                {
                    BienThe = bt,
                    GiaThuong = bt.Thietlapgia
                        .Where(g => g.LoaiGia == "Giá ngày thường")
                        .OrderByDescending(g => g.NgayBatDau)
                        .Select(g => g.Gia)
                        .FirstOrDefault()
                })
                .OrderBy(x => Math.Abs((x.GiaThuong ?? 0) - currentPrice))
                .Take(3)
                .Select(x => x.BienThe)
                .ToList();
            var result = new
            {
                ChiTiet = new
                {
                    bienThe.MaBienThePhong,
                    TenLoai = bienThe.MaLpNavigation.TenLoai,
                    bienThe.TenBienThe,
                    MoTa = bienThe.MaLpNavigation.MoTa,
                    SoNguoiToiDa = bienThe.SoNguoiLon,
                    SoTreEm = bienThe.SoTreEm,
                    bienThe.DienTich,
                    Gia = TinhGiaHienTai(bienThe),
                    SoPhongCon = bienThe.Phongs.Count(p => p.TrangThai == 1),
                    AnhDaiDien = bienThe.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault(),
                    DanhSachAnh = bienThe.Hinhanhs.Select(h => h.DuongDan).ToList(),
                    bienThe.LoaiGiuong,
                    bienThe.HuongNhin,
                    PhanTramGiam = bienThe.ChitietFlashsales
            .Where(fs =>
                fs.Flashsale.TrangThai == true &&
                fs.Flashsale.NgayBatDau <= DateTime.Now &&
                fs.Flashsale.NgayKetThuc >= DateTime.Now
            )
            .Select(fs => fs.PhanTramGiam)
            .FirstOrDefault(),
                    TienIch = bienThe.TienichLoaiphongs.Select(tlp => new
                    {
                        tlp.Tienich.MaTi,
                        tlp.Tienich.TenTienIch,
                        tlp.Tienich.Icon
                    }).ToList()
                },

                BienTheKhac = bienTheKhac.Select(bt => new
                {
                    bt.MaBienThePhong,
                    bt.TenBienThe,
                    bt.LoaiGiuong,
                    SoNguoiToiDa = bt.SoNguoiLon,
                    SoTreEm = bt.SoTreEm,
                    bt.DienTich,
                    bt.HuongNhin,
                    Gia = TinhGiaHienTai(bt),
                    SoPhongCon = bt.Phongs.Count(p => p.TrangThai == 1),
                    AnhDaiDien = bt.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault(),
                    DanhSachAnh = bt.Hinhanhs.Select(h => h.DuongDan).ToList(),
                    PhanTramGiam = bt.ChitietFlashsales
                        .Where(fs =>
                            fs.Flashsale.TrangThai == true &&
                            fs.Flashsale.NgayBatDau <= DateTime.Now &&
                            fs.Flashsale.NgayKetThuc >= DateTime.Now
                        )
                        .Select(fs => fs.PhanTramGiam)
                        .FirstOrDefault(),
                }),

                BienTheNgauNhien = bienTheNgauNhien.Select(bt => new
                {
                    bt.MaBienThePhong,
                    TenLoai = bt.MaLpNavigation.TenLoai,
                    bt.TenBienThe,
                    SoNguoiToiDa = bt.SoNguoiLon,
                    SoTreEm = bt.SoTreEm,
                    bt.DienTich,
                    GiaGoc = TinhGiaHienTai(bt),
                    SoPhongCon = bt.Phongs.Count(p => p.TrangThai == 1),
                    AnhDaiDien = bt.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault(),
                    PhanTramGiam = bt.ChitietFlashsales
                        .Where(fs =>
                            fs.Flashsale.TrangThai == true &&
                            fs.Flashsale.NgayBatDau <= DateTime.Now &&
                            fs.Flashsale.NgayKetThuc >= DateTime.Now
                        )
                        .Select(fs => fs.PhanTramGiam)
                        .FirstOrDefault(),
                })
            };

            return Ok(result);
        }
    }
}

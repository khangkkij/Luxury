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
        [HttpGet("{id}")]
        public IActionResult ChiTiet(int id)
        {
            var phong = _context.LoaiphongBienthes
    .Where(x => x.MaBienThePhong == id)
    .Include(x => x.Hinhanhs)
    .Include(x => x.MaLpNavigation)
    .Include(x => x.Phongs)
    .Include(x => x.Thietlapgia)
    .Include(x => x.TienichLoaiphongs)
        .ThenInclude(tlp => tlp.Tienich)
    .Include(x => x.ChitietFlashsales)
        .ThenInclude(x => x.Flashsale)
    .Select(x => new
    {
        ChiTiet = new
        {
            x.MaBienThePhong,
            TenLoai = x.MaLpNavigation.TenLoai,
            x.TenBienThe,
            MoTa = x.MaLpNavigation.MoTa,
            x.SoNguoiToiDa,
            x.DienTich,

            GiaGoc = x.Thietlapgia
                .Where(g => g.MacDinh == true)
                .Select(g => g.Gia)
                .FirstOrDefault(),

            SoPhongCon = x.Phongs.Count(p => p.TrangThai == 1),

            AnhDaiDien = x.Hinhanhs
                .Select(h => h.DuongDan)
                .FirstOrDefault(),

            DanhSachAnh = x.Hinhanhs
                .Select(h => h.DuongDan)
                .ToList(),

            x.LoaiGiuong,
            x.HuongNhin,

            // ✅ TIỆN ÍCH ĐÚNG KIẾN TRÚC
            TienIch = x.TienichLoaiphongs.Select(tlp => new
            {
                tlp.Tienich.MaTi,
                tlp.Tienich.TenTienIch,
                tlp.Tienich.Icon
            }).ToList()
        },

        BienTheKhac = _context.LoaiphongBienthes
            .Where(bt => bt.MaLp == x.MaLp)
            .Select(bt => new
            {
                bt.MaBienThePhong,
                bt.TenBienThe,
                bt.LoaiGiuong,
                bt.SoNguoiToiDa,
                bt.DienTich,
                Gia = bt.Thietlapgia
                    .Where(g => g.MacDinh == true)
                    .Select(g => g.Gia)
                    .FirstOrDefault(),

                SoPhongCon = _context.Phongs.Count(p =>
                    p.MaBienThePhong == bt.MaBienThePhong &&
                    p.TrangThai == 1
                ),

                AnhDaiDien = bt.Hinhanhs
                    .Select(h => h.DuongDan)
                    .FirstOrDefault(),

                DanhSachAnh = bt.Hinhanhs
                    .Select(h => h.DuongDan)
                    .ToList()
            }).ToList()
    })
    .FirstOrDefault();


            return Ok(phong);
        }
    }
}

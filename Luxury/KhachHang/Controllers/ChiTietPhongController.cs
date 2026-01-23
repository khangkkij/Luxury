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
                .Include(x => x.MaTis)
                .Include(x => x.ChitietFlashsales)
                    .ThenInclude(x => x.Flashsale) 
                .Select(x => new
                {
                    ChiTiet = new
                    {
                        x.MaBienThePhong,
                        TenLoai = x.MaLpNavigation.TenLoai,
                        x.TenBienThe,
                        MoTa=x.MaLpNavigation.MoTa,
                        x.SoNguoiToiDa,
                        x.DienTich,
                        GiaGoc = x.Thietlapgia.Where(g => g.MacDinh == true).Select(g => g.Gia).FirstOrDefault(),
                        PhanTramGiam = x.ChitietFlashsales
                        .Where(fs =>
                            fs.Flashsale.TrangThai == true &&
                            fs.Flashsale.NgayBatDau <= DateTime.Now &&
                            fs.Flashsale.NgayKetThuc >= DateTime.Now
                        )
                        .Select(fs => fs.PhanTramGiam)
                        .FirstOrDefault(),

                        Gia = x.ChitietFlashsales.Any(fs =>
                                fs.Flashsale.TrangThai== true &&
                                fs.Flashsale.NgayBatDau <= DateTime.Now &&
                                fs.Flashsale.NgayKetThuc >= DateTime.Now
                            )
                            ? x.Thietlapgia
                                .Where(g => g.MacDinh == true)
                                .Select(g => g.Gia)
                                .FirstOrDefault()
                              - (
                                x.Thietlapgia
                                    .Where(g => g.MacDinh == true)
                                    .Select(g => g.Gia)
                                    .FirstOrDefault()
                                * x.ChitietFlashsales
                                    .Where(fs =>
                                        fs.Flashsale.TrangThai == true &&
                                        fs.Flashsale.NgayBatDau <= DateTime.Now &&
                                        fs.Flashsale.NgayKetThuc >= DateTime.Now
                                    )
                                    .Select(fs => fs.PhanTramGiam)
                                    .FirstOrDefault() / 100
                              )
                            : x.Thietlapgia
                                .Where(g => g.MacDinh == true)
                                .Select(g => g.Gia)
                                .FirstOrDefault(),

                        SoPhongCon = x.Phongs.Count(p => p.TrangThai == 0),
                        AnhDaiDien = x.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault(),
                        DanhSachAnh = x.Hinhanhs.Select(h => h.DuongDan).ToList(),

                        x.LoaiGiuong,
                        x.HuongNhin,

                        TienIch = x.MaTis.Select(t => new
                        {
                            t.MaTi,
                            t.TenTienIch,
                            t.Icon
                        }).ToList()
                    },

                    BienTheKhac = _context.LoaiphongBienthes
                        .Where(bt => bt.MaLp == x.MaLp && bt.MaBienThePhong != x.MaBienThePhong)
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
                                .FirstOrDefault()
                        })
                        .ToList()
                })
                .FirstOrDefault();

            return Ok(phong);
        }
    }
}

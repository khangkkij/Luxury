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
        [HttpGet]
        public IActionResult GetAll()
        {
            var ds = _context.LoaiphongBienthes.Include(x => x.MaLpNavigation).Include(x => x.Phongs).Include(x => x.Hinhanhs).Include(x => x.Thietlapgia)
                .Include(x => x.ChitietFlashsales).ThenInclude(x => x.Flashsale)
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
                    GiaGoc = x.Thietlapgia.Where(g => g.MacDinh == true).Select(g => g.Gia).FirstOrDefault(),
                    PhanTramGiam = x.ChitietFlashsales
                        .Where(fs =>
                            fs.Flashsale.TrangThai == true &&
                            fs.Flashsale.NgayBatDau <= DateTime.Now &&
                            fs.Flashsale.NgayKetThuc >= DateTime.Now
                        )
                        .Select(fs => fs.PhanTramGiam)
                        .FirstOrDefault(),
                    SoPhongCon = x.Phongs.Count(p => p.TrangThai == 1),
                    AnhDaiDien = x.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault()
                });
            var today = DateTime.Now;
            var dsLoaiPhong = _context.Loaiphongs
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
                    Gia = bt.Thietlapgia
                            .Where(g => g.LoaiGia == "Giá ngày thường" || ((g.LoaiGia == "Lễ" || g.LoaiGia == "Cuối tuần")
                                    && g.NgayBatDau <= today
                                    && g.NgayKetThuc >= today
                                )
                            )
                            .OrderBy(g =>
                                g.LoaiGia == "Lễ" ? 1 :
                                g.LoaiGia == "Cuối tuần" ? 2 :
                                3)
                    .Select(g => g.Gia)
                    .FirstOrDefault(),
                        HinhAnhs = bt.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault(),
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

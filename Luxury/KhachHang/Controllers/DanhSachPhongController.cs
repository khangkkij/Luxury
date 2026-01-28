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
                    SoPhongCon = x.Phongs.Count(p => p.TrangThai == 1),
                    AnhDaiDien = x.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault()
                });
            return Ok(ds);
        }
    }
}

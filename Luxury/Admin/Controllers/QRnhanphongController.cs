using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Luxury.Models;

namespace Luxury.Admin.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class QRnhanphongController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;

        public QRnhanphongController(LuxuryHotelContext context)
        {
            _context = context;
        }

        [HttpGet("scan/{maDatPhong}")]
        public async Task<IActionResult> GetBookingInfoFromQR(int maDatPhong)
        {
            try
            {
                var booking = await _context.Datphongs
                    .Include(dp => dp.MaKhNavigation)
                    .Include(dp => dp.Chitietdatphongs)
                    .Where(dp => dp.MaDatPhong == maDatPhong)
                    .Select(dp => new
                    {
                        maDatPhong = dp.MaDatPhong,
                        tenKhach = dp.MaKhNavigation.HoVaTen,
                        sdt = dp.MaKhNavigation.Sdt, // SQL của bạn là SDT (viết hoa)
                        ngayNhan = dp.Chitietdatphongs.OrderBy(ct => ct.NgayNhan).Select(ct => ct.NgayNhan).FirstOrDefault(),
                        ngayTra = dp.Chitietdatphongs.OrderByDescending(ct => ct.NgayTra).Select(ct => ct.NgayTra).FirstOrDefault(),
                        trangThai = dp.TrangThai,
                        // Tính số phòng chưa được gán mã phòng
                        soPhongCanGan = dp.Chitietdatphongs.Count(ct => ct.MaPhong == null),
                        chiTietDatPhongs = dp.Chitietdatphongs.Select(ct => new {
                            maCtdp = ct.MaCtdp, // SQL là MaCTDP
                            ngayNhan = ct.NgayNhan,
                            ngayTra = ct.NgayTra,
                            maPhong = ct.MaPhong // Nếu cái này khác null, Vue sẽ coi là đã gán xong
                        }).ToList(),
                        phongTrong = _context.Phongs
                            .Where(p => p.TrangThai == 1) // 1 là trạng thái phòng trống trong SQL
                            .Select(p => new {
                                maPhong = p.MaPhong,
                                soPhong = p.SoPhong,
                                tenBienThe = p.MaBienThePhongNavigation.TenBienThe
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (booking == null)
                    return NotFound(new { success = false, message = "Không tìm thấy mã đặt phòng." });

                return Ok(booking);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
using Luxury.KhachHang.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace KhachSanLuxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatPhongController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;

        private const string TrangThaiChoThanhToan = "ChoThanhToan";
        private const string TrangThaiDaXacNhan = "DaXacNhan";

        public DatPhongController(LuxuryHotelContext context)
        {
            _context = context;
        }

        private int? GetMaKhFromCookie()
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var claim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid");
                if (claim == null)
                {
                    return null;
                }
                return int.TryParse(claim.Value, out var id) ? id : (int?)null;
            }
            catch
            {
                return null;
            }
        }

        [HttpPost("tao")]
        public IActionResult TaoDatPhong([FromBody] DatPhongRequest request)
        {
            if (request.MaBienThePhong <= 0)
            {
                return BadRequest("MaBienThePhong không hợp lệ");
            }

            if (request.GiaDat <= 0)
            {
                return BadRequest("GiaDat không hợp lệ");
            }
            if (request.SoLuong <= 0)
            {
                return BadRequest("Số lượng phòng không hợp lệ");
            }

            if (request.NgayTra.Date <= request.NgayNhan.Date)
            {
                return BadRequest("Ngày trả phải sau ngày nhận");
            }

            var soDem = (request.NgayTra.Date - request.NgayNhan.Date).Days;
            if (soDem <= 0)
            {
                return BadRequest("Số đêm không hợp lệ");
            }

            var phongTrong = _context.Phongs
                .Where(p => p.MaBienThePhong == request.MaBienThePhong && p.TrangThai == 1)
                .Take(request.SoLuong)
                .ToList();

            if (phongTrong.Count < request.SoLuong)
            {
                return BadRequest("Không đủ phòng trống");
            }

            var tongTien = request.GiaDat * soDem * request.SoLuong;

            var maKh = request.MaKh ?? GetMaKhFromCookie();

            var datPhong = new Datphong
            {
                MaKh = maKh,
                MaBienThePhong = request.MaBienThePhong,
                NgayDat = DateTime.Now,
                TongTienDuKien = tongTien,
                TrangThai = TrangThaiChoThanhToan
            };

            _context.Datphongs.Add(datPhong);
            _context.SaveChanges();

            foreach (var phong in phongTrong)
            {
                var chiTiet = new Chitietdatphong
                {
                    MaDatPhong = datPhong.MaDatPhong,
                    MaPhong = phong.MaPhong,
                    GiaDat = request.GiaDat,
                    NgayNhan = request.NgayNhan,
                    NgayTra = request.NgayTra,
                    TrangThai = true
                };

                phong.TrangThai = 2;
                _context.Chitietdatphongs.Add(chiTiet);
            }
            _context.SaveChanges();

            return Ok(new
            {
                maDatPhong = datPhong.MaDatPhong,
                tongTien = tongTien,
                soDem = soDem
            });
        }

        [HttpPost("xac-nhan/{maDatPhong}")]
        public IActionResult XacNhanDatPhong(int maDatPhong)
        {
            if (maDatPhong <= 0)
            {
                return BadRequest("MaDatPhong không hợp lệ");
            }

            var datPhong = _context.Datphongs
                .Include(x => x.Hoadons)
                .FirstOrDefault(x => x.MaDatPhong == maDatPhong);

            if (datPhong == null)
            {
                return NotFound("Không tìm thấy đặt phòng");
            }

            var hoaDonHienTai = datPhong.Hoadons.FirstOrDefault();
            if (hoaDonHienTai != null)
            {
                return Ok(new
                {
                    maHd = hoaDonHienTai.MaHd,
                    tongTienPhaiTra = hoaDonHienTai.TongTienPhaiTra
                });
            }

            var tongTien = datPhong.TongTienDuKien ?? 0;
            var hoaDon = new Hoadon
            {
                MaDatPhong = datPhong.MaDatPhong,
                TongTienGoc = tongTien,
                SoTienGiam = 0,
                TongTienPhaiTra = tongTien,
                NgayTao = DateTime.Now
            };

            datPhong.TrangThai = TrangThaiDaXacNhan;
            _context.Hoadons.Add(hoaDon);
            _context.SaveChanges();

            return Ok(new
            {
                maHd = hoaDon.MaHd,
                tongTienPhaiTra = hoaDon.TongTienPhaiTra
            });
        }

        [HttpGet("hoa-don/{maDatPhong}")]
        public IActionResult LayHoaDon(int maDatPhong)
        {
            if (maDatPhong <= 0)
            {
                return BadRequest("MaDatPhong không hợp lệ");
            }

            var datPhong = _context.Datphongs
                .Include(x => x.Hoadons)
                .Include(x => x.Chitietdatphongs)
                    .ThenInclude(x => x.MaPhongNavigation)
                        .ThenInclude(x => x.MaBienThePhongNavigation)
                            .ThenInclude(x => x.MaLpNavigation)
                .Include(x => x.Chitietdatphongs)
                    .ThenInclude(x => x.MaPhongNavigation)
                        .ThenInclude(x => x.MaBienThePhongNavigation)
                            .ThenInclude(x => x.Hinhanhs)
                .FirstOrDefault(x => x.MaDatPhong == maDatPhong);

            if (datPhong == null)
            {
                return NotFound("Không tìm thấy đặt phòng");
            }

            var hoaDon = datPhong.Hoadons.FirstOrDefault();
            if (hoaDon == null)
            {
                return NotFound("Chưa có hóa đơn cho đặt phòng này");
            }

            var chiTiet = datPhong.Chitietdatphongs.FirstOrDefault();
            var bienThe = chiTiet?.MaPhongNavigation?.MaBienThePhongNavigation;

            var ngayNhan = chiTiet?.NgayNhan;
            var ngayTra = chiTiet?.NgayTra;
            int soDem = 0;
            if (ngayNhan.HasValue && ngayTra.HasValue)
            {
                soDem = (ngayTra.Value.Date - ngayNhan.Value.Date).Days;
            }

            var anhDaiDien = bienThe?.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault();

            return Ok(new
            {
                maDatPhong = datPhong.MaDatPhong,
                maHd = hoaDon.MaHd,
                tongTienGoc = hoaDon.TongTienGoc,
                soTienGiam = hoaDon.SoTienGiam,
                tongTienPhaiTra = hoaDon.TongTienPhaiTra,
                ngayTao = hoaDon.NgayTao,
                ngayNhan = ngayNhan,
                ngayTra = ngayTra,
                soDem = soDem,
                giaDat = chiTiet?.GiaDat,
                tenLoai = bienThe?.MaLpNavigation?.TenLoai,
                tenBienThe = bienThe?.TenBienThe,
                anhDaiDien = anhDaiDien
            });
        }
    }
}

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

        private const string TrangThaiChoXacNhan = "ChoXacNhan";

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
            if (request.NgayTra.Date <= request.NgayNhan.Date)
            {
                return BadRequest("Ngày trả phải sau ngày nhận");
            }

            var soDem = (request.NgayTra.Date - request.NgayNhan.Date).Days;
            if (soDem <= 0)
            {
                return BadRequest("Số đêm không hợp lệ");
            }

            var items = request.Items != null && request.Items.Count > 0
                ? request.Items
                : new List<DatPhongItemRequest>
                {
                    new DatPhongItemRequest
                    {
                        MaBienThePhong = request.MaBienThePhong,
                        GiaDat = request.GiaDat,
                        SoLuong = request.SoLuong
                    }
                };

            if (items.Any(x => x.MaBienThePhong <= 0))
            {
                return BadRequest("MaBienThePhong không hợp lệ");
            }
            if (items.Any(x => x.GiaDat <= 0))
            {
                return BadRequest("GiaDat không hợp lệ");
            }
            if (items.Any(x => x.SoLuong <= 0))
            {
                return BadRequest("Số lượng phòng không hợp lệ");
            }
            if ((request.SoNguoiLon ?? 0) < 0 || (request.SoTreEm ?? 0) < 0)
            {
                return BadRequest("Số người không hợp lệ");
            }

            foreach (var item in items)
            {
                var soPhongTrong = _context.Phongs.Count(p =>
                    p.MaBienThePhong == item.MaBienThePhong && p.TrangThai == 1);
                if (soPhongTrong < item.SoLuong)
                {
                    return BadRequest("Không đủ phòng trống");
                }
            }

            var tongTien = items.Sum(x => x.GiaDat * soDem * x.SoLuong);

            // ... (đoạn code kiểm tra số lượng phòng trống giữ nguyên)

            var maKh = request.MaKh ?? GetMaKhFromCookie();
            if (maKh == null || maKh <= 0)
            {
                return Unauthorized("Vui lòng đăng nhập để đặt phòng");
            }

            var datPhong = new Datphong
            {
                MaKh = maKh,
                MaBienThePhong = items.FirstOrDefault()?.MaBienThePhong,
                NgayDat = DateTime.Now,
                TongTienDuKien = tongTien,
                TrangThai = TrangThaiChoXacNhan
            };

            _context.Datphongs.Add(datPhong);
            _context.SaveChanges();

            foreach (var item in items)
            {
                // 1. Tìm danh sách các phòng còn trống (TrangThai == 1) 
                // Lấy đúng số lượng (Take) mà khách đã đặt
                var danhSachPhongTrong = _context.Phongs
                    .Where(p => p.MaBienThePhong == item.MaBienThePhong && p.TrangThai == 1)
                    .Take(item.SoLuong)
                    .ToList();

                if (danhSachPhongTrong.Count < item.SoLuong)
                {
                    return BadRequest($"Phòng thuộc biến thể {item.MaBienThePhong} vừa mới hết.");
                }

                var daGanSoNguoi = false;
                foreach (var phong in danhSachPhongTrong)
                {
                    // 2. Cập nhật trạng thái phòng thành 0 (Đã được đặt) để trừ số lượng phòng trống
                    phong.TrangThai = 1;

                    var ganSoNguoi = !daGanSoNguoi;
                    var chiTiet = new Chitietdatphong
                    {
                        MaDatPhong = datPhong.MaDatPhong,
                        MaPhong = null, // 3. Gán mã phòng cụ thể đã giữ chỗ
                        GiaDat = item.GiaDat,
                        NgayNhan = request.NgayNhan,
                        NgayTra = request.NgayTra,
                        SoNguoiLon = ganSoNguoi ? request.SoNguoiLon : null,
                        SoTreEm = ganSoNguoi ? request.SoTreEm : null,
                        TrangThai = true
                    };

                    if (ganSoNguoi) daGanSoNguoi = true;

                    _context.Chitietdatphongs.Add(chiTiet);
                }
            }

            // 4. Lưu tất cả thay đổi (bao gồm cập nhật TrangThai phòng và thêm chi tiết đặt phòng)
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

            //datPhong.TrangThai = TrangThaiChoXacNhan;
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

        [HttpGet("lich-su")]
        public IActionResult LichSuDatPhong()
        {
            var maKh = GetMaKhFromCookie();
            if (maKh == null || maKh <= 0)
            {
                return Unauthorized("Vui lòng đăng nhập để xem lịch sử.");
            }

            var raw = _context.Datphongs
                .AsNoTracking()
                .Include(x => x.Hoadons)
                .Include(x => x.MaBienThePhongNavigation)
                    .ThenInclude(bt => bt.MaLpNavigation)
                .Include(x => x.Chitietdatphongs)
                    .ThenInclude(ct => ct.MaPhongNavigation)
                        .ThenInclude(p => p.MaBienThePhongNavigation)
                            .ThenInclude(bt => bt.MaLpNavigation)
                .Where(x => x.MaKh == maKh)
                .OrderByDescending(x => x.NgayDat)
                .ToList();

            var data = raw.Select(dp =>
            {
                var chiTiet = dp.Chitietdatphongs.FirstOrDefault();
                var bienThe = chiTiet?.MaPhongNavigation?.MaBienThePhongNavigation ?? dp.MaBienThePhongNavigation;
                var hoaDon = dp.Hoadons.OrderByDescending(h => h.NgayTao).FirstOrDefault();
                var tongTien = hoaDon?.TongTienPhaiTra ?? dp.TongTienDuKien ?? 0;

                return new
                {
                    maDatPhong = dp.MaDatPhong,
                    tenPhong = bienThe?.TenBienThe ?? bienThe?.MaLpNavigation?.TenLoai ?? "Phòng",
                    ngayNhan = chiTiet?.NgayNhan ?? dp.NgayDat,
                    tongTien = tongTien,
                    trangThai = dp.TrangThai ?? string.Empty
                };
            }).ToList();

            return Ok(data);
        }

        [HttpGet("lich-su/{maDatPhong}")]
        public IActionResult LichSuDatPhongChiTiet(int maDatPhong)
        {
            if (maDatPhong <= 0)
            {
                return BadRequest("MaDatPhong không hợp lệ");
            }

            var maKh = GetMaKhFromCookie();
            if (maKh == null || maKh <= 0)
            {
                return Unauthorized("Vui lòng đăng nhập để xem chi tiết.");
            }

            var datPhong = _context.Datphongs
                .AsNoTracking()
                .Include(x => x.MaKhNavigation)
                .Include(x => x.Hoadons)
                .Include(x => x.Chitietdatphongs)
                    .ThenInclude(x => x.MaPhongNavigation)
                        .ThenInclude(x => x.MaBienThePhongNavigation)
                            .ThenInclude(x => x.MaLpNavigation)
                .Include(x => x.Chitietdatphongs)
                    .ThenInclude(x => x.MaPhongNavigation)
                        .ThenInclude(x => x.MaBienThePhongNavigation)
                            .ThenInclude(x => x.Hinhanhs)
                .FirstOrDefault(x => x.MaDatPhong == maDatPhong && x.MaKh == maKh);

            if (datPhong == null)
            {
                return NotFound("Không tìm thấy đặt phòng.");
            }

            var hoaDon = datPhong.Hoadons.OrderByDescending(h => h.NgayTao).FirstOrDefault();
            var chiTiet = datPhong.Chitietdatphongs.FirstOrDefault();
            var bienThe = chiTiet?.MaPhongNavigation?.MaBienThePhongNavigation;

            var ngayNhan = chiTiet?.NgayNhan;
            var ngayTra = chiTiet?.NgayTra;
            int soDem = 0;
            if (ngayNhan.HasValue && ngayTra.HasValue)
            {
                soDem = (ngayTra.Value.Date - ngayNhan.Value.Date).Days;
                if (soDem <= 0) soDem = 1;
            }

            var anhDaiDien = bienThe?.Hinhanhs.Select(h => h.DuongDan).FirstOrDefault();

            return Ok(new
            {
                maDatPhong = datPhong.MaDatPhong,
                ngayDat = datPhong.NgayDat,
                trangThai = datPhong.TrangThai ?? string.Empty,
                hoVaTen = datPhong.MaKhNavigation?.HoVaTen,
                email = datPhong.MaKhNavigation?.Email,
                sdt = datPhong.MaKhNavigation?.Sdt,
                ngayNhan = ngayNhan,
                ngayTra = ngayTra,
                soDem = soDem,
                giaDat = chiTiet?.GiaDat,
                tenLoai = bienThe?.MaLpNavigation?.TenLoai,
                tenBienThe = bienThe?.TenBienThe,
                anhDaiDien = anhDaiDien,
                tongTienGoc = hoaDon?.TongTienGoc ?? datPhong.TongTienDuKien ?? 0,
                soTienGiam = hoaDon?.SoTienGiam ?? 0,
                tongTienPhaiTra = hoaDon?.TongTienPhaiTra ?? datPhong.TongTienDuKien ?? 0
            });
        }

    }
}

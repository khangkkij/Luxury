using Luxury.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Luxury.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VoucherController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public VoucherController(LuxuryHotelContext context) { _context = context; }

        // Helper: Lấy ID khách hàng từ Token
        private int GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdStr, out int userId);
            return userId;
        }

        // 1. Lấy danh sách mã khách hàng CHƯA sở hữu và CÒN HẠN (Trang Săn)
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable(int page = 1, int pageSize = 6)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var now = DateTime.Now;

            // Lọc tại database: Active, chưa hết hạn, còn số lượng và khách chưa thu thập
            var query = _context.Vouchers
                .AsNoTracking()
                .Where(v => v.TrangThai == true
                            && v.NgayKetThuc > now
                            && (v.SoLuong > v.SoLuongDaDung || v.SoLuong == null)
                            && !v.MaKhs.Any(kh => kh.MaKh == userId));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(v => v.NgayKetThuc) // Ưu tiên sắp hết hạn lên trước
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new {
                    v.MaCode,
                    v.MoTa,
                    v.PhanTramGiam,
                    v.SoTienGiam,
                    v.GiaToiThieu,
                    v.NgayKetThuc,
                    v.SoLuong,
                    v.SoLuongDaDung
                })
                .ToListAsync();

            return Ok(new { items, totalCount });
        }

        // 2. Thực hiện lưu mã vào kho (Thu thập mã)
        [HttpPost("collect/{maCode}")]
        public async Task<IActionResult> Collect(string maCode)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var now = DateTime.Now;

            // Load khách hàng và voucher (sử dụng maCode để tìm)
            var user = await _context.Khachhangs
                .Include(kh => kh.MaCodes)
                .FirstOrDefaultAsync(kh => kh.MaKh == userId);

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.MaCode == maCode);

            if (voucher == null || user == null)
                return NotFound(new { message = "Voucher hoặc người dùng không tồn tại." });

            // Kiểm tra các điều kiện logic
            if (voucher.TrangThai == false || (voucher.NgayKetThuc != null && voucher.NgayKetThuc <= now))
                return BadRequest(new { message = "Voucher đã hết hạn hoặc tạm dừng hoạt động." });

            if (voucher.SoLuong != null && voucher.SoLuongDaDung >= voucher.SoLuong)
                return BadRequest(new { message = "Mã giảm giá này đã đạt giới hạn phát hành." });

            if (user.MaCodes.Any(v => v.MaCode == maCode))
                return BadRequest(new { message = "Bạn đã sở hữu mã này trong kho voucher của mình." });

            try
            {
                // Thêm vào bảng trung gian (n-n)
                user.MaCodes.Add(voucher);

                // Tăng số lượng đã dùng/phát hành
                voucher.SoLuongDaDung = (voucher.SoLuongDaDung ?? 0) + 1;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Chúc mừng! Bạn đã thu thập mã thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi cập nhật dữ liệu.", details = ex.Message });
            }
        }

        // 3. Lấy danh sách mã ĐÃ lưu (Kho Voucher của tôi)
        [HttpGet("my-vouchers")]
        public async Task<IActionResult> GetMyVouchers(int page = 1, int pageSize = 5)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var now = DateTime.Now;

            // Tối ưu: Truy vấn trực tiếp từ bảng Vouchers có chứa UserId thay vì load toàn bộ Khachhang
            var query = _context.Vouchers
                .AsNoTracking()
                .Where(v => v.MaKhs.Any(kh => kh.MaKh == userId));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(v => v.NgayKetThuc > now) // Còn hạn lên đầu
                .ThenBy(v => v.NgayKetThuc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new {
                    v.MaCode,
                    v.MoTa,
                    v.PhanTramGiam,
                    v.SoTienGiam,
                    v.GiaToiThieu,
                    v.NgayKetThuc,
                    IsExpired = v.NgayKetThuc < now
                })
                .ToListAsync();

            return Ok(new { items, totalCount });
        }
    }
}
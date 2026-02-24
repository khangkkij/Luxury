using System.Runtime.Intrinsics.Arm;
using Luxury.KhachHang.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Luxury.KhachHang.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class DanhGiaController : Controller
    {
        private readonly LuxuryHotelContext _context;

        public DanhGiaController(LuxuryHotelContext context)
        {
            _context = context;
        }
        private int LayIdTuToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("Token không chứa thông tin ID người dùng");
            }
            return int.Parse(userIdClaim.Value);
        }
        [HttpGet("cho-danh-gia")]
        public async Task<IActionResult> GetChoDanhGia()
        {
            int maKH = LayIdTuToken();
            if (maKH == 0) return Unauthorized(new { message = "Không tìm thấy thông tin khách hàng" });

            var query = await _context.Datphongs
                // 1. Phải Include bảng biến thể để lấy tên, loại giường, hướng nhìn
                .Include(dp => dp.MaBienThePhongNavigation)
                .Where(dp => dp.MaKh == maKH &&
                             _context.Thanhtoans.Any(tt => tt.MaDatPhong == dp.MaDatPhong && tt.TrangThai == "Đã thanh toán") &&
                             !_context.Danhgia.Any(dg => dg.MaDatPhong == dp.MaDatPhong))
                .Select(dp => new
                {
                    dp.MaDatPhong,
                    dp.NgayDat,
                    // Trả về đúng tên để Vue nhận diện (C# tự convert sang camelCase khi trả về JSON)
                    TenBienThe = dp.MaBienThePhongNavigation != null ? dp.MaBienThePhongNavigation.TenBienThe : "Chưa xác định",
                    LoaiGiuong = dp.MaBienThePhongNavigation != null ? dp.MaBienThePhongNavigation.LoaiGiuong : "",
                    HuongNhin = dp.MaBienThePhongNavigation != null ? dp.MaBienThePhongNavigation.HuongNhin : ""
                })
                .ToListAsync();

            return Ok(query);
        }
        [HttpGet("da-danh-gia")]
        public async Task<IActionResult> GetDaDanhGia()
        {
            int maKH = LayIdTuToken();
            if (maKH == 0) return Unauthorized(new { message = "Không tìm thấy thông tin khách hàng" });

            var query = await _context.Danhgia
                .Include(dg => dg.MaDatPhongNavigation)
                    .ThenInclude(dp => dp.MaBienThePhongNavigation)
                .Where(dg => dg.MaDatPhongNavigation.MaKh == maKH)
                .Select(dg => new {
                    dg.MaDg,
                    dg.MaDatPhong,
                    dg.SoSao,
                    dg.NoiDung,
                    dg.NgayDanhGia,
                    dg.HinhAnh,
                    TenPhong = dg.MaDatPhongNavigation.MaBienThePhongNavigation != null ? dg.MaDatPhongNavigation.MaBienThePhongNavigation.TenBienThe : "Chưa xác định",
                    LoaiGiuong = dg.MaDatPhongNavigation.MaBienThePhongNavigation != null ? dg.MaDatPhongNavigation.MaBienThePhongNavigation.LoaiGiuong : "",
                    HuongNhin = dg.MaDatPhongNavigation.MaBienThePhongNavigation != null ? dg.MaDatPhongNavigation.MaBienThePhongNavigation.HuongNhin : ""
                })
                .ToListAsync();

            return Ok(query);
        }
        [HttpPost("gui-danh-gia")]
        public async Task<IActionResult> PostDanhGia([FromForm] Danhgium model, IFormFile? fileAnh)
        {
            int maKH = LayIdTuToken();
            if (maKH == 0) return Unauthorized();

            // KIỂM TRA BẢO MẬT: Đảm bảo đơn hàng này đúng là của ông khách đang đăng nhập
            // Và đơn này PHẢI đã thanh toán thì mới cho phép INSERT vào bảng đánh giá
            var checkHopLe = await _context.Thanhtoans
                .AnyAsync(tt => tt.MaDatPhong == model.MaDatPhong &&
                                tt.MaDatPhongNavigation.MaKh == maKH &&
                                tt.TrangThai == "Đã thanh toán");

            if (!checkHopLe)
            {
                return BadRequest("Bạn không có quyền đánh giá đơn này hoặc đơn hàng chưa hoàn tất thanh toán.");
            }
            if (fileAnh != null && fileAnh.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileAnh.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reviews", fileName);

                // Tạo thư mục nếu chưa có
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await fileAnh.CopyToAsync(stream);
                }
                model.HinhAnh = fileName; // Lưu tên file vào cột HinhAnh trong DB
            }

            // Gán các giá trị tự động mà người dùng không cần nhập
            model.NgayDanhGia = DateTime.Now; // Lưu ngày giờ hiện tại
            model.TrangThai = 1;              // 1: Cho phép hiển thị lên web (0: là ẩn)

            try
            {
                _context.Danhgia.Add(model);      // Thêm đối tượng đánh giá vào hàng chờ
                await _context.SaveChangesAsync(); // Chốt lệnh lưu xuống SQL Server
                return Ok(new { success = true, message = "Cảm ơn bạn đã gửi đánh giá!" });
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu database có vấn đề (vd: sai kiểu dữ liệu, thiếu cột...)
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }
        [HttpDelete("xoa-danh-gia/{id}")]
        public async Task<IActionResult> DeleteDanhGia(int id)
        {
            int maKH = LayIdTuToken();
            if (maKH == 0) return Unauthorized(new { message = "Không tìm thấy thông tin khách hàng" });

            // Tìm đánh giá dựa trên ID và đảm bảo nó thuộc về khách hàng hiện tại
            var danhGia = await _context.Danhgia
                .Include(dg => dg.MaDatPhongNavigation)
                .FirstOrDefaultAsync(dg => dg.MaDg == id && dg.MaDatPhongNavigation.MaKh == maKH);

            if (danhGia == null)
            {
                return NotFound(new { message = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa đánh giá này." });
            }

            try
            {
                if (!string.IsNullOrEmpty(danhGia.HinhAnh))
                {
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reviews", danhGia.HinhAnh);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                _context.Danhgia.Remove(danhGia);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Đã xóa đánh giá thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi hệ thống khi xóa: " + ex.Message);
            }
        }

        [HttpGet("GetByRoom/{variantId}")]
        [AllowAnonymous]
		public async Task<IActionResult> GetReviewsByVariant(int variantId)
		{
			try
			{
				// Truy vấn: 
				// 1. Lấy bảng đánh giá
				// 2. Kèm thông tin Đặt phòng -> Khách hàng (để lấy tên/avatar)
				// 3. Kèm thông tin Phòng (để check xem phòng này có thuộc variantId đang xem không)
				var reviews = await _context.Danhgia
					.Include(dg => dg.MaDatPhongNavigation)
						.ThenInclude(dp => dp.MaKhNavigation) // Join sang bảng Khách hàng
					.Include(dg => dg.MaPhongNavigation)      // Join sang bảng Phòng
					.Where(dg =>
						dg.MaPhongNavigation.MaBienThePhong == variantId && // Chỉ lấy đánh giá thuộc loại phòng này
						dg.TrangThai == 1 // Chỉ lấy đánh giá đã được duyệt (nếu quy định 1 là hiện)
					)
					.OrderByDescending(dg => dg.NgayDanhGia) // Mới nhất lên đầu
					.Select(dg => new DanhGiaViewModel
					{
						MaDg = dg.MaDg,
						SoSao = dg.SoSao ?? 0,
						NoiDung = dg.NoiDung,
						NgayDanhGia = dg.NgayDanhGia ?? DateTime.Now,
						PhanHoi = dg.PhanHoi,

						// Xử lý lấy tên khách hàng an toàn (null check)
						TenKhachHang = dg.MaDatPhongNavigation != null && dg.MaDatPhongNavigation.MaKhNavigation != null
									   ? dg.MaDatPhongNavigation.MaKhNavigation.HoVaTen
									   : "Khách hàng ẩn danh",

						// Lấy avatar, nếu null thì Frontend sẽ tự xử lý avatar mặc định
						Avatar = dg.MaDatPhongNavigation != null && dg.MaDatPhongNavigation.MaKhNavigation != null
								 ? dg.MaDatPhongNavigation.MaKhNavigation.HinhAnh
								 : null
					})
					.ToListAsync();

				return Ok(reviews);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "Lỗi server: " + ex.Message);
			}
		}
	}
    }


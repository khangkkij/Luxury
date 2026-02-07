using Luxury.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Luxury.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KhachHangController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public KhachHangController(LuxuryHotelContext context) { _context = context; }

        public class UpdateProfileDto
        {
            [Required(ErrorMessage = "Họ tên không được để trống")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
            [RegularExpression(@"^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪỬỮỰỵỷỹýỳỹỷỵ\s]+$",
                ErrorMessage = "Họ tên không được chứa số hoặc ký tự đặc biệt")]
            public string? HoVaTen { get; set; }

            [Required(ErrorMessage = "Số điện thoại không được để trống")]
            [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số")]
            public string? Sdt { get; set; }

            [Required(ErrorMessage = "Giới tính là bắt buộc")]
            public string? GioiTinh { get; set; }

            [Required(ErrorMessage = "Số CCCD không được để trống")]
            [RegularExpression(@"^[0-9]{12}$", ErrorMessage = "Số CCCD phải có đúng 12 chữ số")]
            public string? Cccd { get; set; }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            int maKh = int.Parse(userIdStr);
            var user = await _context.Khachhangs
                .AsNoTracking()
                .Where(u => u.MaKh == maKh)
                .Select(u => new {
                    u.MaKh,
                    u.HoVaTen,
                    u.Email,
                    u.Sdt,
                    u.Cccd,
                    u.GioiTinh,
                    u.HinhAnh
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound("Không tìm thấy người dùng.");
            return Ok(user);
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn ảnh.");

            // Đường dẫn vật lý trỏ thẳng vào thư mục USER trong source code Frontend của bạn
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "DuanKhachSan", "KhachHang", "IMG", "USER");

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var extension = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"user_{userId}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            // Ghi đè file ảnh mới vào thư mục
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Cập nhật đường dẫn vào Database (dùng đường dẫn tương đối để Frontend dễ gọi)
            var user = await _context.Khachhangs.FindAsync(userId);
            if (user != null)
            {
                // Lưu path để Frontend có thể map static file hoặc dùng làm URL
                user.HinhAnh = fileName;
                await _context.SaveChangesAsync();
            }

            return Ok(new { fileName = fileName });
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            int userId = int.Parse(userIdStr);
            var user = await _context.Khachhangs.FirstOrDefaultAsync(u => u.MaKh == userId);

            if (user == null) return NotFound("Người dùng không tồn tại.");

            user.HoVaTen = model.HoVaTen;
            user.Sdt = model.Sdt;
            user.GioiTinh = model.GioiTinh;
            user.Cccd = model.Cccd;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật hồ sơ thành công!", newName = user.HoVaTen });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi khi lưu dữ liệu: " + ex.Message);
            }
        }
    }
}
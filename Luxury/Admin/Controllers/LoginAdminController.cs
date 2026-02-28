using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Luxury.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Luxury.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginAdminController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        private readonly IConfiguration _config;

        public LoginAdminController(LuxuryHotelContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
        {
            // 1. Kiểm tra tài khoản
            var nv = await _context.Nhanviens
                .Include(n => n.MaChucVuNavigation)
                .FirstOrDefaultAsync(n => n.TenDangNhap == request.Username);

            if (nv == null)
            {
                // Trả về lỗi 401 kèm thông báo chi tiết
                return Unauthorized(new { success = false, message = "Tài khoản quản trị không tồn tại!" });
            }

            // 2. Kiểm tra mật khẩu
            if (nv.MatKhau != request.Password)
            {
                return Unauthorized(new { success = false, message = "Mật khẩu không chính xác. Vui lòng thử lại!" });
            }

            // 3. Xử lý Token và Cookie (Giữ nguyên logic của bạn)
            var userRole = nv.MaChucVuNavigation?.Role ?? "Staff";
            var token = CreateToken(nv, userRole);

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.Now.AddHours(8)
            });

            return Ok(new
            {
                success = true,
                role = userRole,
                name = nv.HoTenNv,
                message = "Xác thực thành công!"
            });
        }
        private string CreateToken(Nhanvien nv, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nv.HoTenNv),
                new Claim(ClaimTypes.NameIdentifier, nv.MaNv.ToString()),
                new Claim(ClaimTypes.Role, role) // QUAN TRỌNG: Gán Role để khớp với Policy
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Xóa Cookie bằng cách đặt thời gian hết hạn là quá khứ
            Response.Cookies.Delete("AuthToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            return Ok(new { success = true, message = "Đã đăng xuất thành công!" });
        }
    }

    // Đổi tên class request để tránh lỗi xung đột Swagger
    public class AdminLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
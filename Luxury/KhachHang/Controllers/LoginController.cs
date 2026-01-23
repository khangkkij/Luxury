using Microsoft.AspNetCore.Mvc;
using Luxury.Models;
using Luxury.KhachHang.ViewModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography; // Cần cái này để tạo Refresh Token ngẫu nhiên

namespace Luxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        private readonly IConfiguration _configuration;

        public LoginController(LuxuryHotelContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest model)
        {
            // 1. Kiểm tra đầu vào
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.MatKhau))
                return BadRequest("Chưa nhập tài khoản hoặc mật khẩu");

            // 2. Tìm người dùng
            var user = _context.Khachhangs.FirstOrDefault(u => u.Email == model.Email && u.MatKhau == model.MatKhau);

            if (user == null) return Unauthorized("Sai thông tin đăng nhập");
            if (user.TrangThai == false) return StatusCode(403, "Tài khoản bị khóa");

            // 3. TẠO ACCESS TOKEN (JWT) - Sống ngắn (ví dụ 30 phút)
            var accessToken = GenerateJwtToken(user);

            // 4. TẠO REFRESH TOKEN - Sống dài (ví dụ 7 ngày)
            var refreshToken = GenerateRefreshTokenString();

            // 5. LƯU REFRESH TOKEN VÀO DATABASE (Bảng Refeshtoken)
            SaveRefreshTokenToDb(user.MaKh, refreshToken);

            // 6. LƯU CẢ 2 TOKEN VÀO COOKIE
            SetCookie("AuthToken", accessToken, 30); // Access Token hết hạn sau 30 phút
            SetCookie("RefreshToken", refreshToken, 7 * 24 * 60); // Refresh Token hết hạn sau 7 ngày

            return Ok(new { message = "Đăng nhập thành công", data = new { user.HoVaTen, user.Email } });
        }

        // ---------------- CÁC HÀM PHỤ TRỢ ----------------

        // 1. Hàm lưu Token vào bảng Refeshtoken của bạn
        private void SaveRefreshTokenToDb(int userId, string token)
        {
            var rfToken = new Refeshtoken
            {
                UserId = userId,
                Token = token,
                IssuedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(7) // Token này có hạn 7 ngày
            };

            _context.Refeshtokens.Add(rfToken);
            _context.SaveChanges();
        }

        // 2. Hàm tạo chuỗi ngẫu nhiên cho Refresh Token
        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        // 3. Hàm tạo JWT (Access Token)
        private string GenerateJwtToken(dynamic user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaKh.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, "KhachHang")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(30), // Access Token chỉ sống 30 phút
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // 4. Hàm set Cookie chung
        private void SetCookie(string name, string value, int minutes)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Quan trọng: Chống JavaScript đọc trộm
                Expires = DateTime.UtcNow.AddMinutes(minutes),
                Secure = true, // Bắt buộc nếu chạy HTTPS
                SameSite = SameSiteMode.None // Để Vue gọi được API
            };
            Response.Cookies.Append(name, value, cookieOptions);
        }
        [HttpPost("register")] // Link: /api/Login/register
        public IActionResult Register([FromBody] RegisterRequest model)
        {
            // 1. Check lại Valid (dù [ApiController] đã tự check nhưng code thêm cho chắc)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 2. Kiểm tra Email đã tồn tại chưa
            var existingUser = _context.Khachhangs.FirstOrDefault(u => u.Email == model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email này đã được sử dụng!" });
            }

            // 3. Kiểm tra CCCD hoặc SĐT đã tồn tại chưa (Tùy chọn, nên làm để tránh trùng)
            if (_context.Khachhangs.Any(u => u.Cccd == model.Cccd))
            {
                return BadRequest(new { message = "Số CCCD này đã được đăng ký!" });
            }

            // 4. Tạo đối tượng Khachhang mới và Map dữ liệu sang
            var newUser = new Khachhang
            {
                HoVaTen = model.HoVaTen,
                Email = model.Email,
                MatKhau = model.MatKhau, 
                GioiTinh = model.GioiTinh, 
                Sdt = model.Sdt,           
                Cccd = model.Cccd,         
                NgayTao = DateTime.UtcNow,
                TrangThai = true,
                HinhAnh = "default-user.png"
            };

            // 5. Lưu vào Database
            try
            {
                _context.Khachhangs.Add(newUser);
                _context.SaveChanges();

                return Ok(new { message = "Đăng ký thành công! Vui lòng đăng nhập." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }
        // [POST] api/Login/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // 1. Lấy Refresh Token từ Cookie hiện tại (để xóa trong DB nếu cần)
            var refreshToken = Request.Cookies["RefreshToken"];

            // 2. Xóa Token trong Database (Tuỳ chọn: Để bảo mật tuyệt đối)
            // Nếu bạn muốn kỹ tính: Tìm token này trong bảng Refeshtoken và xóa nó đi
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var tokenInDb = _context.Refeshtokens.FirstOrDefault(x => x.Token == refreshToken);
                if (tokenInDb != null)
                {
                    _context.Refeshtokens.Remove(tokenInDb);
                    _context.SaveChanges();
                }
            }

            // 3. Xóa Cookie ở trình duyệt (QUAN TRỌNG NHẤT)
            // Lệnh này sẽ gửi tín hiệu bảo trình duyệt: "Hãy quên 2 cái cookie này đi"
            Response.Cookies.Delete("AuthToken");
            Response.Cookies.Delete("RefreshToken");

            return Ok(new { message = "Đăng xuất thành công" });
        }
    }
}
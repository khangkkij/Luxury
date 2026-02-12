using Luxury.KhachHang.Services;
using Luxury.KhachHang.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Pkcs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography; // Cần cái này để tạo Refresh Token ngẫu nhiên
using System.Text;
using System.Threading.Tasks;

namespace Luxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        private readonly IConfiguration _configuration;
        public readonly IMemoryCache _cache;

        public LoginController(LuxuryHotelContext context, IConfiguration configuration, IMemoryCache cache)
        {
            _context = context;
            _configuration = configuration;
            _cache = cache;
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

            // TẠO ACCESS TOKEN (JWT) - Sống ngắn (30 phút)
            var accessToken = GenerateJwtToken(user);

            // TẠO REFRESH TOKEN - Sống dài ( 7 ngày)
            var refreshToken = GenerateRefreshTokenString();

            // LƯU REFRESH TOKEN VÀO DATABASE (Bảng Refeshtoken)
            SaveRefreshTokenToDb(user.MaKh, refreshToken);

            // LƯU CẢ 2 TOKEN VÀO COOKIE
            SetCookie("AuthToken", accessToken, 30); // Access Token hết hạn sau 30 phút
            SetCookie("RefreshToken", refreshToken, 7 * 24 * 60); // Refresh Token hết hạn sau 7 ngày

            return Ok(new { message = "Đăng nhập thành công", data = new { user.HoVaTen, user.Email } });
        }

        // 1. Hàm lưu Token 
        private void SaveRefreshTokenToDb(int userId, string token)
        {
            var allExistingTokens = _context.Refeshtokens.Where(rt => rt.ExpiredAt < DateTime.UtcNow).ToList();
            if (allExistingTokens.Any())
            {
                _context.Refeshtokens.RemoveRange(allExistingTokens);
            }
            var userOldTokens = _context.Refeshtokens.Where(rt => rt.UserId == userId);
            if (userOldTokens.Any())
            {
                _context.Refeshtokens.RemoveRange(userOldTokens);
            }
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

        // 3. Hàm tạo JWT 
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
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (_context.Khachhangs.Any(u => u.Email == model.Email))
            {
                return BadRequest(new { message = "Email đã được sử dụng" });
            }

            string maOtp = new Random().Next(100000, 999999).ToString();

            var emailService = new EmailService();
            bool GuiThanhCong = await emailService.SendOtpEmail(model.Email, maOtp);
            if (!GuiThanhCong)
            {
                return StatusCode(500, new { message = "Địa chỉ Gmail này không tồn tại thực tế!" });
            }
            var dateTam = new { UserInfo = model, Otp = maOtp };
            _cache.Set(model.Email, dateTam, TimeSpan.FromMinutes(10));


            return Ok(new { message = "Mã xác thực đã được gửi đến Gmail của bạn." });
        }
        [HttpPut("verify-otp")]
        public IActionResult VerifyOtp(string email, string otpKhachNhap)
        {
            if (_cache.TryGetValue(email, out dynamic data))
            {
                try
                {
                    string otpgoc = data.Otp.ToString();

                    if (otpKhachNhap.Trim() == otpgoc.Trim())
                    {
                        var info = data.UserInfo;
                        var newUser = new Khachhang
                        {
                            HoVaTen = info.HoVaTen,
                            Email = info.Email,
                            MatKhau = info.MatKhau,
                            GioiTinh = info.GioiTinh,
                            Sdt = info.Sdt,
                            Cccd = info.Cccd,
                            NgayTao = DateTime.UtcNow,
                            TrangThai = true,
                            HinhAnh = "default-user.png"
                        };
                        _context.Khachhangs.Add(newUser);
                        _context.SaveChanges();
                        _cache.Remove(email);
                        return Ok(new { message = "Đăng ký thành công." });
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ." });
                }
            }
            return BadRequest(new { message = "Mã OTP không chính xác hoặc đã hết hạn (10 phút)." });
        }
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Email không được để trống." });
            }

            // 3. Kiểm tra thông tin trong Cache
            if (_cache.TryGetValue(email, out dynamic data))
            {
                try
                {
                    // 4. Tạo mã OTP mới
                    string maOtpMoi = new Random().Next(100000, 999999).ToString();

                    // 5. Gửi email (Dùng await thay vì .Result để tránh deadlock)
                    var emailService = new EmailService();
                    bool guiThanhCong = await emailService.SendOtpEmail(email, maOtpMoi);

                    if (!guiThanhCong)
                    {
                        return StatusCode(500, new { message = "Không thể gửi email lúc này. Vui lòng thử lại!" });
                    }

                    // 6. Cập nhật lại Cache với mã mới và gia hạn thêm 10 phút
                    var updatedData = new { UserInfo = data.UserInfo, Otp = maOtpMoi };
                    _cache.Set(email, updatedData, TimeSpan.FromMinutes(10));

                    return Ok(new { message = "Mã OTP mới đã được gửi đến Gmail của bạn." });
                }
                catch (Exception)
                {
                    return StatusCode(500, new { message = "Lỗi hệ thống khi gửi lại mã." });
                }
            }

            // 7. Trường hợp Cache đã mất (quá 10 phút)
            return BadRequest(new { message = "Phiên làm việc đã hết hạn. Vui lòng thực hiện đăng ký lại!" });
        }
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // 1. Lấy Refresh Token từ Cookie hiện tại (để xóa trong DB nếu cần)
            var refreshToken = Request.Cookies["RefreshToken"];

            // 2. Xóa Token trong Database (Tuỳ chọn: Để bảo mật tuyệt đối)11
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
            Response.Cookies.Delete("AuthToken");
            Response.Cookies.Delete("RefreshToken");

            return Ok(new { message = "Đăng xuất thành công" });
        }
        [HttpGet("facebook-login")]
        public IActionResult FacebookLogin()
        {
            // Yêu cầu Facebook xác thực
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("FacebookResponse")
            };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet("facebook-response")]
        public async Task<IActionResult> FacebookResponse()
        {
            // 1. Lấy dữ liệu từ Facebook
            var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);
            if (!result.Succeeded) return BadRequest("Lỗi xác thực Facebook");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;

            // 2. Lấy ID Facebook 
            var fbId = claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            // 3. Lấy Email - NẾU TRỐNG THÌ DÙNG ID ĐỂ TẠO EMAIL GIẢ
            var email = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                // Tạo email dạng: 901331519065056@facebook.com
                email = $"{fbId}@facebook.com";
            }

            var name = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

            // 4. Kiểm tra hoặc Tạo mới khách hàng
            var user = _context.Khachhangs.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                user = new Khachhang
                {
                    Email = email,
                    HoVaTen = name,
                    MatKhau = "FB_" + fbId,
                    NgayTao = DateTime.UtcNow,
                    TrangThai = true,
                    HinhAnh = "default-user.png"
                };
                _context.Khachhangs.Add(user);
                _context.SaveChanges();
            }

            // 5. Tạo Token và Set Cookie (Giữ các hàm cũ của bạn)
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshTokenString();
            SaveRefreshTokenToDb(user.MaKh, refreshToken);

            SetCookie("AuthToken", accessToken, 30);
            SetCookie("RefreshToken", refreshToken, 7 * 24 * 60);

            // 6. Redirect về Vue kèm thông tin để Vue lưu vào LocalStorage
            var encodedName = Uri.EscapeDataString(user.HoVaTen);
            var encodedEmail = Uri.EscapeDataString(email);
            return Redirect($"http://localhost:5173/?login_success=true&name={encodedName}&email={encodedEmail}&provider=facebook");
        }


        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var XacThuc = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse")
            };
            return Challenge(XacThuc, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        }
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            // Lấy dữ liệu từ Google
            var KetQua = await HttpContext.AuthenticateAsync(Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
            if (!KetQua.Succeeded) return BadRequest("Lỗi xác thực từ phía Google");

            var DanhSachThongTin = KetQua.Principal.Identities.FirstOrDefault()?.Claims;

            var GoogleId = DanhSachThongTin?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = DanhSachThongTin?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            var name = DanhSachThongTin?.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email)) return BadRequest("Không lấy được email từ tài khoản Google.");

            var KhachHang = _context.Khachhangs.FirstOrDefault(u => u.Email == email);
            if (KhachHang == null)
            {
                KhachHang = new Khachhang
                {
                    Email = email,
                    HoVaTen = name,
                    MatKhau = "GG_" + GoogleId,
                    NgayTao = DateTime.UtcNow,
                    TrangThai = true,
                    HinhAnh = "default-user.png"
                };
                _context.Khachhangs.Add(KhachHang);
                _context.SaveChanges();
            }
            var maAccessToken = GenerateJwtToken(KhachHang);
            var maRefreshToken = GenerateRefreshTokenString();
            SaveRefreshTokenToDb(KhachHang.MaKh, maRefreshToken);
            SetCookie("AuthToken", maAccessToken, 30);
            SetCookie("RefreshToken", maRefreshToken, 7 * 24 * 60);
            var tenMaHoa = Uri.EscapeDataString(KhachHang.HoVaTen ?? "");
            var emailMaHoa = Uri.EscapeDataString(email ?? "");
            return Redirect($"http://localhost:5173/?login_success=true&name={tenMaHoa}&email={emailMaHoa}&provider=google");
        }
        // 1. Gửi OTP quên mật khẩu
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromQuery] string email)
        {
            var khachHang = _context.Khachhangs.FirstOrDefault(u => u.Email == email);
            if (khachHang == null)
            {
                return BadRequest(new { message = "Email này chưa được đăng ký tài khoản!" });
            }

            string maOtp = new Random().Next(100000, 999999).ToString();
            var emailService = new EmailService();
            bool guiThanhCong = await emailService.SendOtpEmail(email, maOtp);

            if (guiThanhCong)
            {
                // Lưu OTP vào Cache, đánh dấu đây là luồng "QuenMatKhau"
                _cache.Set("Reset_" + email, maOtp, TimeSpan.FromMinutes(10));
                return Ok(new { message = "Mã xác thực đã được gửi để đặt lại mật khẩu." });
            }
            return StatusCode(500, new { message = "Lỗi gửi mail." });
        }

        // 2. Xác thực OTP và Đổi mật khẩu mới
        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest model)
        {
            if (_cache.TryGetValue("Reset_" + model.Email, out string otpLuuTrongCache))
            {
                if (otpLuuTrongCache == model.Otp)
                {
                    var khachHang = _context.Khachhangs.FirstOrDefault(u => u.Email == model.Email);
                    if (khachHang != null)
                    {
                        khachHang.MatKhau = model.NewPassword; // Nên mã hóa mật khẩu ở đây
                        _context.SaveChanges();
                        _cache.Remove("Reset_" + model.Email); // Xóa cache sau khi xong
                        return Ok(new { message = "Đổi mật khẩu thành công!" });
                    }
                }
                return BadRequest(new { message = "Mã OTP không chính xác." });
            }
            return BadRequest(new { message = "Mã OTP đã hết hạn." });
        }
    }
}
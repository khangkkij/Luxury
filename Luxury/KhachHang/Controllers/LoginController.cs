using Luxury.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhachSanLuxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]   
    public class LoginController : Controller
    {
        private readonly LuxuryHotelContext _context;
        public LoginController(LuxuryHotelContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Set<Khachhang>()
                .FirstOrDefaultAsync(x => x.Email == request.Email && x.MatKhau == request.Password);

            if (user == null)
                return Unauthorized("Email hoặc mật khẩu không đúng.");

            // LƯU COOKIE
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,        // Bảo mật: JS không đọc được
                Secure = true,          // Chỉ gửi qua HTTPS
                SameSite = SameSiteMode.Strict, // Chống CSRF
                Expires = DateTime.UtcNow.AddDays(7) // Cookie sống trong 7 ngày
            };

            // Lưu Tên và ID vào Cookie
            Response.Cookies.Append("user_name", user.HoVaTen, cookieOptions);
            Response.Cookies.Append("user_id", user.MaKh.ToString(), cookieOptions);

            return Ok(new
            {
                FullName = user.HoVaTen,
                Id = user.MaKh
            });
        }
    }
}

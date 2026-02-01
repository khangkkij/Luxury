using Luxury.KhachHang.ViewModel;
using Luxury.Services;
using Luxury.Models; // Nhớ using namespace chứa ContactDto
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

[Route("api/[controller]")]
[ApiController]
public class LienHeController : ControllerBase
{
	private readonly EmailService _emailService;
	private readonly IMemoryCache _cache;


	// Chỉ Inject EmailService, KHÔNG cần AppDbContext nữa
	public LienHeController(EmailService emailService, IMemoryCache cache)
	{
		_emailService = emailService;
		_cache = cache;
	}

	[HttpPost]
	public async Task<IActionResult> GuiLienHe([FromBody] ContactDTO model)
	{

		// Xử lý trường hợp IP bị null (dùng "unknown" để không bị lỗi code)
		var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
		string cacheKey = $"SpamBlock_{ipAddress}";

		// Kiểm tra xem đã bị chặn chưa
		if (_cache.TryGetValue(cacheKey, out _))
		{
			return BadRequest(new { message = "Bạn thao tác quá nhanh! Vui lòng quay lại sau 5 phút." });
		}

		// Validate dữ liệu đầu vào
		if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.NoiDung))
		{
			return BadRequest(new { message = "Vui lòng nhập Email và Nội dung" });
		}

		// === CHIẾN THUẬT MỚI: CHẶN TRƯỚC ===
		// Đặt lệnh cấm ngay lập tức để chặn các click liên tiếp
		var cacheOptions = new MemoryCacheEntryOptions()
			.SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // <--- ĐỔI THÀNH 5 PHÚT

		_cache.Set(cacheKey, true, cacheOptions);

		try
		{
			// 3. Gửi Email
			string adminEmail = "hieudvpk04071@gmail.com";
			string subject = $"[Khách vãng lai] {model.HoTen} nhắn tin";

			string body = $@"
            <div style='border: 1px solid #ddd; padding: 15px;'>
                <h3 style='color: #f35525;'>Tin nhắn từ Website</h3>
                <p><strong>Họ tên:</strong> {model.HoTen}</p>
                <p><strong>Email:</strong> <a href='mailto:{model.Email}'>{model.Email}</a></p>
                <p><strong>IP:</strong> {ipAddress}</p> 
                <p><strong>Thời gian:</strong> {DateTime.Now}</p> 
                <p><strong>Tiêu đề:</strong> {model.TieuDe}</p>
                <p>{model.NoiDung}</p>
            </div>
        ";

			await _emailService.SendEmailAsync(
				toEmail: adminEmail,
				subject: subject,
				messageBody: body,
				replyToEmail: model.Email,
				replyToName: model.HoTen
			);

			return Ok(new { message = "Gửi tin nhắn thành công!" });
		}
		catch (Exception ex)
		{
			// === NẾU GỬI LỖI THÌ GỠ CHẶN ===
			// Để khách còn có cơ hội thử lại (ví dụ do mạng lag)
			_cache.Remove(cacheKey);

			return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
		}
	}
}
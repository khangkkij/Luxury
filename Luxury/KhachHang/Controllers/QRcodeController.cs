using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using MailKit.Net.Smtp;
using MimeKit;
using Luxury.KhachHang.Services;
using Luxury.Models;

namespace Luxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QRcodeController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;

        public QRcodeController(LuxuryHotelContext context)
        {
            _context = context;
        }

        [HttpPost("send-confirmation/{maDatPhong}")]
        public async Task<IActionResult> SendConfirmation(string maDatPhong)
        {
            try
            {
                if (!int.TryParse(maDatPhong, out int id))
                {
                    return BadRequest(new { success = false, message = "Mã đặt phòng không hợp lệ." });
                }

                // Lấy thông tin khách hàng từ DB
                var datPhong = await _context.Datphongs
                    .Include(dp => dp.MaKhNavigation)
                    .FirstOrDefaultAsync(dp => dp.MaDatPhong == id);

                if (datPhong == null || datPhong.MaKhNavigation == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy thông tin đặt phòng hoặc email khách hàng." });
                }

                string emailKhach = datPhong.MaKhNavigation.Email;
                string tenKhach = datPhong.MaKhNavigation.HoVaTen;

                // 1. Tạo QR Code chứa mã đặt phòng
                byte[] qrBytes;
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrData = qrGenerator.CreateQrCode(maDatPhong, QRCodeGenerator.ECCLevel.Q);
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrData))
                    {
                        qrBytes = qrCode.GetGraphic(20);
                    }
                }

                // 2. Cấu hình Email dựa trên EmailService đã chạy được của bạn
                var message = new MimeMessage();
                string senderEmail = "khangpbpk04248@gmail.com";
                string appPassword = "ddoy sdxo ikpv ssqc"; // Mật khẩu ứng dụng đã xác thực

                message.From.Add(new MailboxAddress("Luxury Hotel", senderEmail));
                message.To.Add(new MailboxAddress(tenKhach, emailKhach));
                message.Subject = $"Xác Nhận Đặt Phòng Thành Công - Mã QR #{maDatPhong}";

                var bodyBuilder = new BodyBuilder();
                string qrCid = MimeKit.Utils.MimeUtils.GenerateMessageId();
                var linkedResource = bodyBuilder.LinkedResources.Add("qrcode.png", qrBytes, new ContentType("image", "png"));
                linkedResource.ContentId = qrCid;

                // Sử dụng template HTML từ EmailQR
                bodyBuilder.HtmlBody = EmailQR.GetOnlyQRBody(tenKhach, maDatPhong, qrCid);
                message.Body = bodyBuilder.ToMessageBody();

                // 3. Gửi Mail qua SMTP (Dùng cấu hình cổng 587 StartTls như file mẫu)
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(senderEmail, appPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return Ok(new { success = true, message = "Đã gửi mã QR về Gmail thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
	using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
	using MimeKit;
namespace Luxury.Services
{

	public class EmailService
	{
		private readonly IConfiguration _config;

		// Inject IConfiguration thay vì IOptions
		public EmailService(IConfiguration config)
		{
			_config = config;
		}

		public async Task SendEmailAsync(string toEmail, string subject, string messageBody, string replyToEmail = null, string replyToName = null)
		{
			var emailSettings = _config.GetSection("EmailSettings");

			var message = new MimeMessage();

			// 1. NGƯỜI GỬI: Vẫn phải là Email hệ thống (để không bị chặn)
			message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));

			// 2. NGƯỜI NHẬN: Lễ tân hoặc Quản lý
			message.To.Add(new MailboxAddress("", toEmail));

			// 3. REPLY-TO: ĐÂY LÀ MẤU CHỐT
			// Nếu có email khách hàng, gán vào đây. Khi admin bấm Reply, nó sẽ rep vào email này.
			if (!string.IsNullOrEmpty(replyToEmail))
			{
				message.ReplyTo.Add(new MailboxAddress(replyToName ?? replyToEmail, replyToEmail));
			}

			message.Subject = subject;

			var bodyBuilder = new BodyBuilder();
			bodyBuilder.HtmlBody = messageBody;
			message.Body = bodyBuilder.ToMessageBody();

			using (var client = new MailKit.Net.Smtp.SmtpClient())
			{
				try
				{
					// Kết nối tới Gmail (Port 587 là chuẩn cho StartTls)
					await client.ConnectAsync(
						emailSettings["MailServer"],
						int.Parse(emailSettings["MailPort"]),
						SecureSocketOptions.StartTls
					);

					// Đăng nhập
					await client.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["Password"]);

					// Gửi
					await client.SendAsync(message);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Lỗi gửi mail: " + ex.Message);
					throw; // Ném lỗi ra để Controller biết
				}
				finally
				{
					// Ngắt kết nối sạch sẽ
					await client.DisconnectAsync(true);
				}
			}
		}
		
	}
}

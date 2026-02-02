using MailKit.Net.Smtp;
using MimeKit;

namespace Luxury.KhachHang.Services
{
    public class EmailService
    {
        public async Task<bool> SendOtpEmail(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Luxury Hotel", "khangpbpk04248@gmail.com"));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = " Mã xác nhận của bạn";
            message.Body = new TextPart("html")
            {
                Text = $@"<h3>Xác thực tài khoản Luxury Hotel</h3>
                          <p>Mã OTP của bạn là: <b style='color:blue; font-size:18px;'>{otp}</b></p>
                          <p>Mã này có hiệu lực trong 10 phút. Vui lòng không cung cấp mã này cho bất kỳ ai.</p>"
            };
            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("khangpbpk04248@gmail.com", "ddoy sdxo ikpv ssqc");
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    return true;
                }
                catch
                (Exception ex)
                {
                    return false;
                }

            }
        }
    }
}//q i v x z o r m f c b p p j b t

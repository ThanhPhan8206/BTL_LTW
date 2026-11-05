using System.Net;
using System.Net.Mail;
using System.Text;

namespace QLSanBong.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gửi email chứa mã xác thực OTP 6 chữ số khôi phục mật khẩu qua Gmail SMTP.
        /// </summary>
        public async Task SendOtpEmailAsync(string toEmail, string userName, string otpCode, int expireMinutes = 5)
        {
            var subject = $"[UTC STADIUM] Mã xác thực OTP khôi phục mật khẩu: {otpCode}";
            var htmlBody = $@"
                <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 560px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0, 43, 91, 0.08);'>
                    <div style='background-color: #002B5B; color: #ffffff; padding: 24px; text-align: center;'>
                        <h2 style='margin: 0; font-size: 22px; letter-spacing: 0.5px;'>UTC STADIUM</h2>
                        <p style='margin: 4px 0 0 0; color: #FFC107; font-size: 12px; font-weight: bold; text-transform: uppercase;'>Hệ Thống Đặt Sân Bóng Đá Sinh Viên</p>
                    </div>
                    <div style='padding: 28px 24px; background-color: #ffffff; color: #1e293b;'>
                        <p style='margin-top: 0;'>Xin chào <strong>{WebUtility.HtmlEncode(userName)}</strong>,</p>
                        <p style='line-height: 1.6;'>Hệ thống nhận được yêu cầu đặt lại mật khẩu cho tài khoản gắn với địa chỉ email <strong>{WebUtility.HtmlEncode(toEmail)}</strong>. Vui lòng sử dụng mã OTP 6 chữ số dưới đây để xác thực:</p>
                        <div style='margin: 24px 0; padding: 18px; background-color: #F4F7FB; border: 2px dashed #FFC107; border-radius: 12px; text-align: center;'>
                            <span style='font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #002B5B;'>{otpCode}</span>
                        </div>
                        <p style='font-size: 13px; color: #64748b; line-height: 1.6;'>
                            * Mã OTP này có hiệu lực trong vòng <strong>{expireMinutes} phút</strong>.<br/>
                            * Tuyệt đối không chia sẻ mã này cho bất kỳ ai để bảo vệ tài khoản của bạn.
                        </p>
                    </div>
                    <div style='background-color: #f8fafc; padding: 14px 24px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0;'>
                        &copy; 2026 UTC STADIUM Security &bull; Trường Đại học Giao thông Vận tải
                    </div>
                </div>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        /// <summary>
        /// Hàm gửi email tổng quát qua cấu hình EmailSettings trong appsettings.json.
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.TryParse(_configuration["EmailSettings:SmtpPort"], out int port) ? port : 587;
            var senderEmail = (_configuration["EmailSettings:SenderEmail"] ?? string.Empty).Trim();
            var senderName = _configuration["EmailSettings:SenderName"] ?? "UTC STADIUM Security";
            var senderAppPassword = (_configuration["EmailSettings:SenderAppPassword"] ?? string.Empty).Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderAppPassword))
            {
                throw new InvalidOperationException("Chưa cấu hình đầy đủ SenderEmail hoặc SenderAppPassword trong appsettings.json (mục EmailSettings).");
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = htmlBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            mailMessage.To.Add(new MailAddress(toEmail.Trim()));

            using var smtpClient = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail, senderAppPassword),
                Timeout = 20000
            };

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MessengerAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken)
        {
            var resetUrl = $"http://localhost:5173/reset-password?token={resetToken}&email={Uri.EscapeDataString(email)}";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Мессенджер", _config["EmailSettings:From"]));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Восстановление пароля";

            message.Body = new TextPart("html")
            {
                Text = $@"
            <h2>Восстановление пароля</h2>
            <p>Перейдите по <a href='{resetUrl}'>ссылке</a> для смены пароля</p>
            <p>Ссылка действительна 15 минут</p>
            <hr>
            <small>Если вы не запрашивали восстановление, проигнорируйте это письмо</small>
            "
            };

            using var client = new SmtpClient();

            await client.ConnectAsync(
    _config["EmailSettings:SmtpServer"],
    int.Parse(_config["EmailSettings:SmtpPort"]),
    SecureSocketOptions.StartTls); // ← ВАЖНО!

            await client.AuthenticateAsync(
    _config["EmailSettings:Username"],
    _config["EmailSettings:Password"]);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}

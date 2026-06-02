using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DermaApp.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("DermaMind", _config["Email:From"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Your OTP Code - DermaMind";
            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>DermaMind - Password Reset</h2>
                    <p>Your OTP code is:</p>
                    <h1 style='color:blue'>{otp}</h1>
                    <p>This code expires in 10 minutes.</p>"
            };

            using var client = new SmtpClient();

            await client.ConnectAsync(
          _config["Email:Host"],
           int.Parse(_config["Email:Port"]),
            SecureSocketOptions.SslOnConnect); // ✅ SSL بدل StartTLS

            await client.AuthenticateAsync(
                _config["Email:From"],
                _config["Email:Password"]);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
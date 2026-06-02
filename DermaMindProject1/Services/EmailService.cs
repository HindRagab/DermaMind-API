using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Task = System.Threading.Tasks.Task;

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
            message.From.Add(new MailboxAddress("DermaMind", "hindragab72@gmail.com"));
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
            await client.ConnectAsync("smtp-relay.brevo.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Brevo:SmtpLogin"], _config["Brevo:SmtpKey"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
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
            Configuration.Default.ApiKey["api-key"] = _config["Brevo:ApiKey"];

            var apiInstance = new TransactionalEmailsApi();

            var sendSmtpEmail = new SendSmtpEmail(
            sender: new SendSmtpEmailSender("hindragab72@gmail.com", "DermaMind"), to: new List<SendSmtpEmailTo> { new SendSmtpEmailTo(toEmail) },
                subject: "Your OTP Code - DermaMind",
                htmlContent: $@"
                    <h2>DermaMind - Password Reset</h2>
                    <p>Your OTP code is:</p>
                    <h1 style='color:blue'>{otp}</h1>
                    <p>This code expires in 10 minutes.</p>"
            );

            await System.Threading.Tasks.Task.Run(() => apiInstance.SendTransacEmail(sendSmtpEmail));
        }
    }
}
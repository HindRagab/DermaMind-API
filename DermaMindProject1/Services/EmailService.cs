using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace DermaApp.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private static readonly HttpClient _httpClient = new HttpClient();

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            var apiKey = _config["Brevo:ApiKey"];

            var payload = new
            {
                sender = new
                {
                    name = "DermaMind",
                    email = "hindragab72@gmail.com" // لازم يكون نفس الإيميل المسجل/المفعّل كـ Sender في Brevo
                },
                to = new[]
                {
                    new { email = toEmail }
                },
                subject = "Your OTP Code - DermaMind",
                htmlContent = $@"
                    <h2>DermaMind - Password Reset</h2>
                    <p>Your OTP code is:</p>
                    <h1 style='color:blue'>{otp}</h1>
                    <p>This code expires in 10 minutes.</p>"
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Brevo API error: {(int)response.StatusCode} - {errorBody}");
            }
        }
    }
}
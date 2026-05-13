using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiBaseUrl = "https://derma-mind-api-production.up.railway.app";

        public ChatbotController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // ✅ إرسال رسالة للـ Chatbot
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatbotRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto.Message))
                return BadRequest(new { message = "Please enter a message" });

            var requestBody = new
            {
                message = dto.Message,
                history = dto.History ?? new List<object>(),
                diagnosis_context = dto.DiagnosisContext ?? ""
            };

            var response = await _httpClient.PostAsync(
                $"{_aiBaseUrl}/chat",
                new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"));

            var result = await response.Content.ReadAsStringAsync();
            return Ok(result);
        }
    }

    public class ChatbotRequestDto
    {
        public string Message { get; set; }
        public List<object>? History { get; set; }
        public string? DiagnosisContext { get; set; }
    }
}
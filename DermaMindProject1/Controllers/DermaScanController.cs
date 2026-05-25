using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DermaScanController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiBaseUrl = "https://derma-mind-api-production-a4c0.up.railway.app";
        public DermaScanController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // ✅ تحليل صورة البشرة
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeSkin(
            IFormFile image,
            [FromForm] string? skin_type = null,
            [FromForm] string? medical_history = null)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Please upload an image" });

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
                return BadRequest(new { message = "Only JPG and PNG images are allowed" });

            try
            {
                using var content = new MultipartFormDataContent();

                using var stream = image.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                content.Add(streamContent, "image", image.FileName);

                if (!string.IsNullOrEmpty(skin_type))
                    content.Add(new StringContent(skin_type), "skin_type");

                if (!string.IsNullOrEmpty(medical_history))
                    content.Add(new StringContent(medical_history), "medical_history");

                var response = await _httpClient.PostAsync($"{_aiBaseUrl}/analyze", content);
                var resultString = await response.Content.ReadAsStringAsync();
                var resultJson = JsonSerializer.Deserialize<JsonElement>(resultString);
                return Ok(resultJson);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "AI service error", error = ex.Message });
            }
        }

        // ✅ Health Check
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var response = await _httpClient.GetAsync(_aiBaseUrl);
                var result = await response.Content.ReadAsStringAsync();
                var resultJson = JsonSerializer.Deserialize<JsonElement>(result);
                return Ok(resultJson);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "AI service unreachable", error = ex.Message });
            }
        }
    }
}
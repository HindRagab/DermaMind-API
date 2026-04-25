using Microsoft.AspNetCore.Mvc;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DermaScanController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiBaseUrl = "https://derma-mind-api-production.up.railway.app";

        public DermaScanController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // ✅ تحليل صورة البشرة
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeSkin(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Please upload an image" });

            try
            {
                // بنبعت الصورة للـ Python API
                using var content = new MultipartFormDataContent();
                using var stream = image.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                content.Add(streamContent, "image", image.FileName);

                var response = await _httpClient.PostAsync($"{_aiBaseUrl}/predict", content);
                var result = await response.Content.ReadAsStringAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "AI service error", error = ex.Message });
            }
        }

        // ✅ Health Check للـ AI
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            var response = await _httpClient.GetAsync(_aiBaseUrl);
            var result = await response.Content.ReadAsStringAsync();
            return Ok(result);
        }
    }
}
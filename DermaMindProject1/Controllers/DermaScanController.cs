using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using DermaApp.API.Data;
using DermaApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DermaScanController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiBaseUrl = "https://derma-mind-api-production-a4c0.up.railway.app";
        private readonly AppDbContext _context;

        public DermaScanController(IHttpClientFactory httpClientFactory, AppDbContext context)
        {
            _httpClient = httpClientFactory.CreateClient();
            _context = context;
        }

        // ✅ تحليل صورة البشرة
        [HttpPost("analyze")]
        [Authorize]
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

                // ✅ حفظ النتيجة في البروفايل
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                string? diagnosis = null;
                if (resultJson.TryGetProperty("diagnosis", out var diagProp))
                    diagnosis = diagProp.GetString();

                _context.DermaScanResults.Add(new DermaScanResult
                {
                    UserId = userId ?? "",
                    ResultJson = resultString,
                    Diagnosis = diagnosis
                });
                await _context.SaveChangesAsync();

                return Ok(resultJson);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "AI service error", error = ex.Message });
            }
        }

        // ✅ بدء التشخيص التفاعلي
        [HttpPost("diagnose/start")]
        public async Task<IActionResult> DiagnoseStart(
            IFormFile image,
            [FromForm] string? lang = "ar",
            [FromForm] string? medical_history = null)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Please upload an image" });
            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = image.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                content.Add(streamContent, "image", image.FileName);
                content.Add(new StringContent(lang ?? "ar"), "lang");
                if (!string.IsNullOrEmpty(medical_history))
                    content.Add(new StringContent(medical_history), "medical_history");
                var response = await _httpClient.PostAsync($"{_aiBaseUrl}/diagnose/start", content);
                var resultString = await response.Content.ReadAsStringAsync();
                var resultJson = JsonSerializer.Deserialize<JsonElement>(resultString);
                return Ok(resultJson);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "AI service error", error = ex.Message });
            }
        }

        // ✅ إتمام التشخيص النهائي
        [HttpPost("diagnose/complete")]
        public async Task<IActionResult> DiagnoseComplete([FromBody] JsonElement dto)
        {
            try
            {
                var content = new StringContent(
                    dto.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json");
                var response = await _httpClient.PostAsync($"{_aiBaseUrl}/diagnose/complete", content);
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

        // ✅ جلب تاريخ السكانات بتاعت اليوزر
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetScanHistory()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var scans = await _context.DermaScanResults
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var result = scans.Select(s => new
            {
                s.Id,
                s.Diagnosis,
                s.CreatedAt,
                Result = JsonSerializer.Deserialize<JsonElement>(s.ResultJson)
            });

            return Ok(result);
        }

        // ✅ جلب سكان واحد بالتفصيل
        [HttpGet("history/{id}")]
        [Authorize]
        public async Task<IActionResult> GetScanById(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var scan = await _context.DermaScanResults
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (scan == null)
                return NotFound(new { message = "Scan not found" });

            return Ok(new
            {
                scan.Id,
                scan.Diagnosis,
                scan.CreatedAt,
                Result = JsonSerializer.Deserialize<JsonElement>(scan.ResultJson)
            });
        }
    }
}
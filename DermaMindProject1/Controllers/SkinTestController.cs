using DermaApp.API.Data;
using DermaApp.API.DTOs;
using DermaApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkinTestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SkinTestController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ جلب كل الأسئلة
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions()
        {
            var questions = await _context.SkinTestQuestions
                .Include(q => q.Options)
                .Select(q => new SkinTestQuestionDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    Options = q.Options.Select(o => new SkinTestOptionDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        SkinTypePoint = o.SkinTypePoint
                    }).ToList()
                }).ToListAsync();

            return Ok(questions);
        }

        // ✅ Submit الإجابات وبيرجع نوع البشرة
        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitTest(SubmitSkinTestDto dto)
        {
            var selectedOptions = await _context.SkinTestOptions
                .Where(o => dto.SelectedOptionIds.Contains(o.Id))
                .ToListAsync();

            // حساب نوع البشرة
            var skinTypeCounts = selectedOptions
                .GroupBy(o => o.SkinTypePoint)
                .ToDictionary(g => g.Key, g => g.Count());

            var skinType = skinTypeCounts.OrderByDescending(x => x.Value)
                .FirstOrDefault().Key ?? "Normal";

            // حفظ النتيجة
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = new SkinTestResult
            {
                UserId = userId,
                SkinType = skinType
            };

            _context.SkinTestResults.Add(result);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                skinType,
                message = $"Your skin type is {skinType}"
            });
        }

        // ✅ جلب نتيجة المستخدم
        [HttpGet("my-result")]
        [Authorize]
        public async Task<IActionResult> GetMyResult()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _context.SkinTestResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.TakenAt)
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound(new { message = "No test result found" });

            return Ok(new { result.SkinType, result.TakenAt });
        }

        // ✅ Admin - إضافة سؤال جديد
        [HttpPost("add-question")]
        public async Task<IActionResult> AddQuestion(SkinTestQuestionDto dto)
        {
            var question = new SkinTestQuestion
            {
                QuestionText = dto.QuestionText,
                Options = dto.Options.Select(o => new SkinTestOption
                {
                    OptionText = o.OptionText,
                    SkinTypePoint = o.SkinTypePoint
                }).ToList()
            };

            _context.SkinTestQuestions.Add(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Question added successfully!" });
        }
    }
}
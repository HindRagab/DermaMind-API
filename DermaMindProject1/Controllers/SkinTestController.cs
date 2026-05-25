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

        // ✅ Seed الأسئلة
        [HttpPost("seed-questions")]
        public async Task<IActionResult> SeedQuestions()
        {
            if (await _context.SkinTestQuestions.AnyAsync())
                return BadRequest(new { message = "Questions already exist!" });

            var questions = new List<SkinTestQuestion>
            {
                new SkinTestQuestion
                {
                    QuestionText = "كيف تبدو بشرتك بعد ساعتين من غسلها بدون أي منتجات؟",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "جافة ومشدودة", SkinTypePoint = "Dry" },
                        new SkinTestOption { OptionText = "طبيعية ومريحة", SkinTypePoint = "Normal" },
                        new SkinTestOption { OptionText = "دهنية في كل الوجه", SkinTypePoint = "Oily" },
                        new SkinTestOption { OptionText = "دهنية في منطقة T فقط", SkinTypePoint = "Combination" }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "كيف تتفاعل بشرتك مع المنتجات الجديدة؟",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "تتهيج وتحمر بسهولة", SkinTypePoint = "Sensitive" },
                        new SkinTestOption { OptionText = "لا تتفاعل عادةً", SkinTypePoint = "Normal" },
                        new SkinTestOption { OptionText = "تظهر حبوب أحياناً", SkinTypePoint = "Oily" },
                        new SkinTestOption { OptionText = "تجف وتتقشر", SkinTypePoint = "Dry" }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "ما حجم مسام بشرتك؟",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "مسام صغيرة جداً لا تكاد تُرى", SkinTypePoint = "Dry" },
                        new SkinTestOption { OptionText = "مسام متوسطة الحجم", SkinTypePoint = "Normal" },
                        new SkinTestOption { OptionText = "مسام كبيرة وواضحة", SkinTypePoint = "Oily" },
                        new SkinTestOption { OptionText = "كبيرة في منطقة T وصغيرة في الخدين", SkinTypePoint = "Combination" }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "كيف تبدو بشرتك عند النظر في المرآة؟",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "مطفية وبدون لمعة", SkinTypePoint = "Dry" },
                        new SkinTestOption { OptionText = "مشرقة وطبيعية", SkinTypePoint = "Normal" },
                        new SkinTestOption { OptionText = "لامعة ودهنية", SkinTypePoint = "Oily" },
                        new SkinTestOption { OptionText = "لامعة في المنتصف وعادية على الجانبين", SkinTypePoint = "Combination" }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "هل تعانين من الجفاف أو التقشر؟",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "نعم دائماً", SkinTypePoint = "Dry" },
                        new SkinTestOption { OptionText = "أحياناً في الشتاء فقط", SkinTypePoint = "Normal" },
                        new SkinTestOption { OptionText = "نادراً جداً", SkinTypePoint = "Oily" },
                        new SkinTestOption { OptionText = "في بعض المناطق فقط", SkinTypePoint = "Combination" }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "كيف تتأثر بشرتك بأشعة الشمس؟",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "تحترق بسرعة وتحمر", SkinTypePoint = "Sensitive" },
                        new SkinTestOption { OptionText = "تحترق أحياناً ثم تعود طبيعية", SkinTypePoint = "Normal" },
                        new SkinTestOption { OptionText = "تتحول للون بني بسهولة", SkinTypePoint = "Oily" },
                        new SkinTestOption { OptionText = "نادراً ما تتأثر", SkinTypePoint = "Combination" }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "هل تلاحظين خطوط أو تجاعيد مبكرة في بشرتك؟",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "نعم وبشكل واضح", SkinTypePoint = "Dry" },
                        new SkinTestOption { OptionText = "بدأت تظهر قليلاً", SkinTypePoint = "Normal" },
                        new SkinTestOption { OptionText = "لا تقريباً", SkinTypePoint = "Oily" },
                        new SkinTestOption { OptionText = "في مناطق معينة فقط", SkinTypePoint = "Combination" }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "ما الذي يصف بشرتك بعد التعرض للبرد أو الرياح؟",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "تجف وتتشقق بسهولة", SkinTypePoint = "Dry" },
                        new SkinTestOption { OptionText = "تتهيج وتحمر", SkinTypePoint = "Sensitive" },
                        new SkinTestOption { OptionText = "لا تتأثر كثيراً", SkinTypePoint = "Oily" },
                        new SkinTestOption { OptionText = "تجف في الخدين فقط", SkinTypePoint = "Combination" }
                    }
                }
            };

            _context.SkinTestQuestions.AddRange(questions);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Questions seeded successfully!", count = questions.Count });
        }
    }
}
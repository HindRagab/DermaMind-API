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

        // ✅ جلب كل الأسئلة (بدعم اللغة: ar / en)
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions([FromQuery] string lang = "ar")
        {
            var questions = await _context.SkinTestQuestions
                .Include(q => q.Options)
                .ToListAsync();

            var result = questions.Select(q => new SkinTestQuestionDto
            {
                Id = q.Id,
                QuestionText = lang == "en" && !string.IsNullOrEmpty(q.QuestionTextEn)
                    ? q.QuestionTextEn
                    : q.QuestionText,
                Category = q.Category,
                Options = q.Options.Select(o => new SkinTestOptionDto
                {
                    Id = o.Id,
                    OptionText = lang == "en" && !string.IsNullOrEmpty(o.OptionTextEn)
                        ? o.OptionTextEn
                        : o.OptionText,
                    Score = o.Score
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        // ✅ Submit الإجابات وحساب كود نوع البشرة الكامل (4 محاور)
        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitTest(SubmitSkinTestDto dto, [FromQuery] string lang = "ar")
        {
            // نجيب الخيارات المختارة مع السؤال المرتبط بيها عشان نعرف الـ Category
            var selectedOptions = await _context.SkinTestOptions
                .Include(o => o.Question)
                .Where(o => dto.SelectedOptionIds.Contains(o.Id))
                .ToListAsync();

            if (!selectedOptions.Any())
                return BadRequest(new { message = "No valid options selected" });

            // تجميع الدرجات حسب كل محور
            int odScore = selectedOptions.Where(o => o.Question.Category == "OD").Sum(o => o.Score);
            int srScore = selectedOptions.Where(o => o.Question.Category == "SR").Sum(o => o.Score);
            int pnScore = selectedOptions.Where(o => o.Question.Category == "PN").Sum(o => o.Score);
            int wtScore = selectedOptions.Where(o => o.Question.Category == "WT").Sum(o => o.Score);

            // تطبيق العتبات حسب الورقة البحثية
            // O/D: المجموع <= 10 يبقى Dry، أكبر من 10 يبقى Oily
            char odLetter = odScore <= 10 ? 'D' : 'O';

            // الباقي: المجموع >= 7.5 يبقى الصفة الأولى، أقل يبقى التانية
            char srLetter = srScore >= 7.5 ? 'S' : 'R';
            char pnLetter = pnScore >= 7.5 ? 'P' : 'N';
            char wtLetter = wtScore >= 7.5 ? 'W' : 'T';

            string skinTypeCode = $"{odLetter}{srLetter}{pnLetter}{wtLetter}";

            // هات الوصف والاستراتيجية من الجدول المرجعي
            var profile = await _context.SkinTypeProfiles
                .FirstOrDefaultAsync(p => p.Code == skinTypeCode);

            // حفظ النتيجة
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = new SkinTestResult
            {
                UserId = userId,
                SkinTypeCode = skinTypeCode,
                OD_Score = odScore,
                SR_Score = srScore,
                PN_Score = pnScore,
                WT_Score = wtScore
            };

            _context.SkinTestResults.Add(result);
            await _context.SaveChangesAsync();

            string description = lang == "en" && !string.IsNullOrEmpty(profile?.DescriptionEn)
                ? profile.DescriptionEn
                : (profile?.Description ?? "Description not available");

            string strategy = lang == "en" && !string.IsNullOrEmpty(profile?.StrategyEn)
                ? profile.StrategyEn
                : (profile?.Strategy ?? "Strategy not available");

            return Ok(new SkinTestResultDto
            {
                SkinTypeCode = skinTypeCode,
                OD_Score = odScore,
                SR_Score = srScore,
                PN_Score = pnScore,
                WT_Score = wtScore,
                Description = description,
                Strategy = strategy,
                TakenAt = result.TakenAt
            });
        }

        // ✅ جلب نتيجة المستخدم
        [HttpGet("my-result")]
        [Authorize]
        public async Task<IActionResult> GetMyResult([FromQuery] string lang = "ar")
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _context.SkinTestResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.TakenAt)
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound(new { message = "No test result found" });

            var profile = await _context.SkinTypeProfiles
                .FirstOrDefaultAsync(p => p.Code == result.SkinTypeCode);

            string description = lang == "en" && !string.IsNullOrEmpty(profile?.DescriptionEn)
                ? profile.DescriptionEn
                : (profile?.Description ?? "Description not available");

            string strategy = lang == "en" && !string.IsNullOrEmpty(profile?.StrategyEn)
                ? profile.StrategyEn
                : (profile?.Strategy ?? "Strategy not available");

            return Ok(new SkinTestResultDto
            {
                SkinTypeCode = result.SkinTypeCode,
                OD_Score = result.OD_Score,
                SR_Score = result.SR_Score,
                PN_Score = result.PN_Score,
                WT_Score = result.WT_Score,
                Description = description,
                Strategy = strategy,
                TakenAt = result.TakenAt
            });
        }

        [HttpDelete("delete-questions")]
        public async Task<IActionResult> DeleteQuestions()
        {
            var options = await _context.SkinTestOptions.ToListAsync();
            _context.SkinTestOptions.RemoveRange(options);

            var questions = await _context.SkinTestQuestions.ToListAsync();
            _context.SkinTestQuestions.RemoveRange(questions);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Questions deleted!" });
        }

        // ✅ Admin - إضافة سؤال جديد
        [HttpPost("add-question")]
        public async Task<IActionResult> AddQuestion(SkinTestQuestionDto dto)
        {
            var question = new SkinTestQuestion
            {
                QuestionText = dto.QuestionText,
                Category = dto.Category,
                Options = dto.Options.Select(o => new SkinTestOption
                {
                    OptionText = o.OptionText,
                    Score = o.Score
                }).ToList()
            };

            _context.SkinTestQuestions.Add(question);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Question added successfully!" });
        }

        // ✅ Seed الأسئلة الـ13 حسب نظام Baumann (BSTS) - عربي وإنجليزي
        [HttpPost("seed-questions")]
        public async Task<IActionResult> SeedQuestions()
        {
            if (await _context.SkinTestQuestions.AnyAsync())
                return BadRequest(new { message = "Questions already exist!" });

            var questions = new List<SkinTestQuestion>
            {
                // ===== القسم الأول: O/D - الدهون مقابل الجفاف (4 أسئلة) =====
                new SkinTestQuestion
                {
                    QuestionText = "بعد غسل الوجه دون استخدام مرطب، كيف تشعر ببشرتك بعد ساعتين؟",
                    QuestionTextEn = "After washing your face without using a moisturizer, how does your skin feel after two hours?",
                    Category = "OD",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "خشنة جداً أو متقشرة", OptionTextEn = "Very rough or flaky", Score = 1 },
                        new SkinTestOption { OptionText = "مشدودة", OptionTextEn = "Tight", Score = 2 },
                        new SkinTestOption { OptionText = "طبيعية", OptionTextEn = "Normal", Score = 3 },
                        new SkinTestOption { OptionText = "لامعة أو دهنية", OptionTextEn = "Shiny or oily", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "عند التقاط الصور الفوتوغرافية، هل يبدو وجهك لامعاً؟",
                    QuestionTextEn = "In photographs, does your face appear shiny?",
                    Category = "OD",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "أبداً", OptionTextEn = "Never", Score = 1 },
                        new SkinTestOption { OptionText = "أحياناً", OptionTextEn = "Sometimes", Score = 2 },
                        new SkinTestOption { OptionText = "غالباً", OptionTextEn = "Often", Score = 3 },
                        new SkinTestOption { OptionText = "دائماً", OptionTextEn = "Always", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "كيف يبدو الوجه أو المكياج بعد 3 ساعات من وضعه؟",
                    QuestionTextEn = "How does your face or makeup look 3 hours after application?",
                    Category = "OD",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "متشقق أو متكتل", OptionTextEn = "Cracked or caked", Score = 1 },
                        new SkinTestOption { OptionText = "طبيعي أو ناعم", OptionTextEn = "Normal or smooth", Score = 2 },
                        new SkinTestOption { OptionText = "لامع", OptionTextEn = "Shiny", Score = 3 },
                        new SkinTestOption { OptionText = "سائل أو دهني بشكل زائد", OptionTextEn = "Runny or excessively oily", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "ما حجم مسام الوجه عند النظر في المرآة؟",
                    QuestionTextEn = "How large are your facial pores when you look in the mirror?",
                    Category = "OD",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "صغيرة جداً أو غير مرئية", OptionTextEn = "Very small or invisible", Score = 1 },
                        new SkinTestOption { OptionText = "متوسطة", OptionTextEn = "Medium", Score = 2 },
                        new SkinTestOption { OptionText = "كبيرة", OptionTextEn = "Large", Score = 3 },
                        new SkinTestOption { OptionText = "كبيرة جداً وواضحة", OptionTextEn = "Very large and visible", Score = 4 }
                    }
                },

                // ===== القسم الثاني: S/R - الحساسية مقابل المقاومة (3 أسئلة) =====
                new SkinTestQuestion
                {
                    QuestionText = "هل تعاني من ظهور بثور أو حبوب حمراء؟",
                    QuestionTextEn = "Do you experience breakouts or red pimples?",
                    Category = "SR",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "أبداً", OptionTextEn = "Never", Score = 1 },
                        new SkinTestOption { OptionText = "نادراً", OptionTextEn = "Rarely", Score = 2 },
                        new SkinTestOption { OptionText = "مرة واحدة شهرياً على الأقل", OptionTextEn = "At least once a month", Score = 3 },
                        new SkinTestOption { OptionText = "مرة أسبوعياً على الأقل", OptionTextEn = "At least once a week", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "هل تسبب منتجات العناية بالبشرة حكة أو احمراراً؟",
                    QuestionTextEn = "Do skincare products cause itching or redness?",
                    Category = "SR",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "أبداً", OptionTextEn = "Never", Score = 1 },
                        new SkinTestOption { OptionText = "نادراً", OptionTextEn = "Rarely", Score = 2 },
                        new SkinTestOption { OptionText = "غالباً", OptionTextEn = "Often", Score = 3 },
                        new SkinTestOption { OptionText = "دائماً", OptionTextEn = "Always", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "هل تم تشخيصك سابقاً بحب الشباب أو الوردية أو الأكزيما؟",
                    QuestionTextEn = "Have you ever been diagnosed with acne, rosacea, or eczema?",
                    Category = "SR",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "لا", OptionTextEn = "No", Score = 1 },
                        new SkinTestOption { OptionText = "يُقال إن لدي بشرة حساسة", OptionTextEn = "I've been told I have sensitive skin", Score = 2 },
                        new SkinTestOption { OptionText = "نعم", OptionTextEn = "Yes", Score = 3 },
                        new SkinTestOption { OptionText = "نعم، وبشكل مزمن أو شديد", OptionTextEn = "Yes, chronically or severely", Score = 4 }
                    }
                },

                // ===== القسم الثالث: P/N - التصبغ مقابل عدم التصبغ (3 أسئلة) =====
                new SkinTestQuestion
                {
                    QuestionText = "بعد اختفاء الحبوب أو الجروح، هل تترك أثراً داكناً؟",
                    QuestionTextEn = "After pimples or wounds heal, do they leave a dark mark?",
                    Category = "PN",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "أبداً", OptionTextEn = "Never", Score = 1 },
                        new SkinTestOption { OptionText = "أحياناً", OptionTextEn = "Sometimes", Score = 2 },
                        new SkinTestOption { OptionText = "غالباً", OptionTextEn = "Often", Score = 3 },
                        new SkinTestOption { OptionText = "دائماً", OptionTextEn = "Always", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "عدد البقع الداكنة الحالية في الوجه؟",
                    QuestionTextEn = "How many dark spots do you currently have on your face?",
                    Category = "PN",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "لا يوجد", OptionTextEn = "None", Score = 1 },
                        new SkinTestOption { OptionText = "قليلة (1-5)", OptionTextEn = "Few (1-5)", Score = 2 },
                        new SkinTestOption { OptionText = "متوسطة (6-15)", OptionTextEn = "Moderate (6-15)", Score = 3 },
                        new SkinTestOption { OptionText = "كثيرة جداً (أكثر من 16)", OptionTextEn = "Very many (more than 16)", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "عند التعرض للشمس دون حماية، ماذا يحدث؟",
                    QuestionTextEn = "When exposed to the sun without protection, what happens?",
                    Category = "PN",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "احمرار فقط دون اسمرار", OptionTextEn = "Redness only, no tanning", Score = 1 },
                        new SkinTestOption { OptionText = "احمرار ثم اسمرار", OptionTextEn = "Redness then tanning", Score = 2 },
                        new SkinTestOption { OptionText = "اسمرار فقط", OptionTextEn = "Tanning only", Score = 3 },
                        new SkinTestOption { OptionText = "اسمرار سريع ولون داكن جداً", OptionTextEn = "Quick and very dark tanning", Score = 4 }
                    }
                },

                // ===== القسم الرابع: W/T - التجاعيد مقابل المرونة (3 أسئلة) =====
                new SkinTestQuestion
                {
                    QuestionText = "هل تدخن السجائر؟",
                    QuestionTextEn = "Do you smoke cigarettes?",
                    Category = "WT",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "لا", OptionTextEn = "No", Score = 1 },
                        new SkinTestOption { OptionText = "توقفت سابقاً", OptionTextEn = "I used to but quit", Score = 2 },
                        new SkinTestOption { OptionText = "نعم، بشكل محدود", OptionTextEn = "Yes, occasionally", Score = 3 },
                        new SkinTestOption { OptionText = "نعم، يومياً", OptionTextEn = "Yes, daily", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "ما مدى استخدامك لواقي الشمس؟",
                    QuestionTextEn = "How often do you use sunscreen?",
                    Category = "WT",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "يومياً", OptionTextEn = "Daily", Score = 1 },
                        new SkinTestOption { OptionText = "غالباً", OptionTextEn = "Often", Score = 2 },
                        new SkinTestOption { OptionText = "أحياناً", OptionTextEn = "Sometimes", Score = 3 },
                        new SkinTestOption { OptionText = "أبداً", OptionTextEn = "Never", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "ما لون بشرتك الطبيعي دون التعرض للشمس؟",
                    QuestionTextEn = "What is your natural skin color without sun exposure?",
                    Category = "WT",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "بني غامق أو أسود", OptionTextEn = "Dark brown or black", Score = 1 },
                        new SkinTestOption { OptionText = "بني متوسط", OptionTextEn = "Medium brown", Score = 2 },
                        new SkinTestOption { OptionText = "بني فاتح أو بيج", OptionTextEn = "Light brown or beige", Score = 3 },
                        new SkinTestOption { OptionText = "أبيض جداً أو شاحب", OptionTextEn = "Very fair or pale", Score = 4 }
                    }
                }
            };

            _context.SkinTestQuestions.AddRange(questions);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Questions seeded successfully!", count = questions.Count });
        }

        // ✅ Seed جدول الأنواع الـ16 (الوصف والاستراتيجية حسب الورقة البحثية) - عربي وإنجليزي
        [HttpPost("seed-skin-types")]
        public async Task<IActionResult> SeedSkinTypes()
        {
            if (await _context.SkinTypeProfiles.AnyAsync())
                return BadRequest(new { message = "Skin type profiles already exist!" });

            var profiles = new List<SkinTypeProfile>
            {
                new SkinTypeProfile
                {
                    Code = "ORNT",
                    Description = "دهنية - مقاومة - غير مصبغة - مشدودة",
                    DescriptionEn = "Oily - Resistant - Non-pigmented - Tight",
                    Strategy = "بشرة متوازنة وقوية؛ يوصى بتنظيف لطيف وروتين بسيط للحفاظ على التوازن.",
                    StrategyEn = "Balanced and resilient skin; a gentle cleanse and simple routine is recommended to maintain balance."
                },
                new SkinTypeProfile
                {
                    Code = "ORPT",
                    Description = "دهنية - مقاومة - مصبغة - مشدودة",
                    DescriptionEn = "Oily - Resistant - Pigmented - Tight",
                    Strategy = "التركيز على تفتيح البقع باستخدام فيتامين C مع حماية منتظمة من الشمس.",
                    StrategyEn = "Focus on brightening dark spots with Vitamin C alongside regular sun protection."
                },
                new SkinTypeProfile
                {
                    Code = "ORPW",
                    Description = "دهنية - مقاومة - مصبغة - مجعدة",
                    DescriptionEn = "Oily - Resistant - Pigmented - Wrinkled",
                    Strategy = "روتين مضاد للشيخوخة يشمل واقي الشمس ومضادات أكسدة مثل الريتينول.",
                    StrategyEn = "An anti-aging routine including sunscreen and antioxidants such as retinol."
                },
                new SkinTypeProfile
                {
                    Code = "ORNW",
                    Description = "دهنية - مقاومة - غير مصبغة - مجعدة",
                    DescriptionEn = "Oily - Resistant - Non-pigmented - Wrinkled",
                    Strategy = "يمكنها تحمّل علاجات التجاعيد القوية مع الحفاظ على ترطيب معتدل.",
                    StrategyEn = "Can tolerate strong anti-wrinkle treatments while maintaining moderate hydration."
                },
                new SkinTypeProfile
                {
                    Code = "OSNT",
                    Description = "دهنية - حساسة - غير مصبغة - مشدودة",
                    DescriptionEn = "Oily - Sensitive - Non-pigmented - Tight",
                    Strategy = "تهدئة الالتهاب أولاً باستخدام مكونات مهدئة مع التحكم في الدهون.",
                    StrategyEn = "Calm inflammation first using soothing ingredients while controlling oil."
                },
                new SkinTypeProfile
                {
                    Code = "OSPT",
                    Description = "دهنية - حساسة - مصبغة - مشدودة",
                    DescriptionEn = "Oily - Sensitive - Pigmented - Tight",
                    Strategy = "علاج التصبغ بمواد غير مهيجة مثل حمض الأزيليك مع حماية شمسية صارمة.",
                    StrategyEn = "Treat pigmentation with gentle ingredients like azelaic acid alongside strict sun protection."
                },
                new SkinTypeProfile
                {
                    Code = "OSPW",
                    Description = "دهنية - حساسة - مصبغة - مجعدة",
                    DescriptionEn = "Oily - Sensitive - Pigmented - Wrinkled",
                    Strategy = "من أكثر الأنواع تعقيداً؛ يتطلب روتيناً دقيقاً وإشرافاً طبياً مستمراً.",
                    StrategyEn = "One of the most complex types; requires a careful routine and ongoing medical supervision."
                },
                new SkinTypeProfile
                {
                    Code = "OSNW",
                    Description = "دهنية - حساسة - غير مصبغة - مجعدة",
                    DescriptionEn = "Oily - Sensitive - Non-pigmented - Wrinkled",
                    Strategy = "استخدام الريتينول بتركيزات منخفضة مع مكونات مضادة للالتهاب.",
                    StrategyEn = "Use low-concentration retinol alongside anti-inflammatory ingredients."
                },
                new SkinTypeProfile
                {
                    Code = "DRNT",
                    Description = "جافة - مقاومة - غير مصبغة - مشدودة",
                    DescriptionEn = "Dry - Resistant - Non-pigmented - Tight",
                    Strategy = "دعم حاجز البشرة بترطيب عميق غني بالسيراميدات.",
                    StrategyEn = "Support the skin barrier with deep hydration rich in ceramides."
                },
                new SkinTypeProfile
                {
                    Code = "DRPT",
                    Description = "جافة - مقاومة - مصبغة - مشدودة",
                    DescriptionEn = "Dry - Resistant - Pigmented - Tight",
                    Strategy = "الجمع بين الترطيب والتفتيح باستخدام أحماض الفواكه الخفيفة (AHAs).",
                    StrategyEn = "Combine hydration and brightening using mild fruit acids (AHAs)."
                },
                new SkinTypeProfile
                {
                    Code = "DRPW",
                    Description = "جافة - مقاومة - مصبغة - مجعدة",
                    DescriptionEn = "Dry - Resistant - Pigmented - Wrinkled",
                    Strategy = "روتين شامل: ترطيب مكثف + تفتيح + مضادات شيخوخة.",
                    StrategyEn = "A comprehensive routine: intensive hydration + brightening + anti-aging."
                },
                new SkinTypeProfile
                {
                    Code = "DRNW",
                    Description = "جافة - مقاومة - غير مصبغة - مجعدة",
                    DescriptionEn = "Dry - Resistant - Non-pigmented - Wrinkled",
                    Strategy = "ترطيب منتظم مع برامج مبكرة لمكافحة الشيخوخة.",
                    StrategyEn = "Regular hydration along with early anti-aging programs."
                },
                new SkinTypeProfile
                {
                    Code = "DSNT",
                    Description = "جافة - حساسة - غير مصبغة - مشدودة",
                    DescriptionEn = "Dry - Sensitive - Non-pigmented - Tight",
                    Strategy = "بشرة شديدة التفاعل؛ تحتاج منتجات Hypoallergenic وترطيب مكثف.",
                    StrategyEn = "Highly reactive skin; needs hypoallergenic products and intensive hydration."
                },
                new SkinTypeProfile
                {
                    Code = "DSPT",
                    Description = "جافة - حساسة - مصبغة - مشدودة",
                    DescriptionEn = "Dry - Sensitive - Pigmented - Tight",
                    Strategy = "علاج التصبغ بحذر شديد مع الحفاظ على ترطيب دائم.",
                    StrategyEn = "Treat pigmentation very cautiously while maintaining constant hydration."
                },
                new SkinTypeProfile
                {
                    Code = "DSPW",
                    Description = "جافة - حساسة - مصبغة - مجعدة",
                    DescriptionEn = "Dry - Sensitive - Pigmented - Wrinkled",
                    Strategy = "أولوية لتقوية الحاجز الجلدي قبل البدء في علاج التجاعيد.",
                    StrategyEn = "Priority on strengthening the skin barrier before starting wrinkle treatment."
                },
                new SkinTypeProfile
                {
                    Code = "DSNW",
                    Description = "جافة - حساسة - غير مصبغة - مجعدة",
                    DescriptionEn = "Dry - Sensitive - Non-pigmented - Wrinkled",
                    Strategy = "مكافحة التجاعيد بمواد لطيفة مثل Peptides مع ترطيب عميق.",
                    StrategyEn = "Combat wrinkles with gentle ingredients like peptides alongside deep hydration."
                }
            };

            _context.SkinTypeProfiles.AddRange(profiles);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Skin type profiles seeded successfully!", count = profiles.Count });
        }
    }
}
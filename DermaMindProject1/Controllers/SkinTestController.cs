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
                    Category = q.Category,
                    Options = q.Options.Select(o => new SkinTestOptionDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        Score = o.Score
                    }).ToList()
                }).ToListAsync();

            return Ok(questions);
        }

        // ✅ Submit الإجابات وحساب كود نوع البشرة الكامل (4 محاور)
        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitTest(SubmitSkinTestDto dto)
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

            return Ok(new SkinTestResultDto
            {
                SkinTypeCode = skinTypeCode,
                OD_Score = odScore,
                SR_Score = srScore,
                PN_Score = pnScore,
                WT_Score = wtScore,
                Description = profile?.Description ?? "Description not available",
                Strategy = profile?.Strategy ?? "Strategy not available",
                TakenAt = result.TakenAt
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

            var profile = await _context.SkinTypeProfiles
                .FirstOrDefaultAsync(p => p.Code == result.SkinTypeCode);

            return Ok(new SkinTestResultDto
            {
                SkinTypeCode = result.SkinTypeCode,
                OD_Score = result.OD_Score,
                SR_Score = result.SR_Score,
                PN_Score = result.PN_Score,
                WT_Score = result.WT_Score,
                Description = profile?.Description ?? "Description not available",
                Strategy = profile?.Strategy ?? "Strategy not available",
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

        // ✅ Seed الأسئلة الـ13 حسب نظام Baumann (BSTS)
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
                    Category = "OD",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "خشنة جداً أو متقشرة", Score = 1 },
                        new SkinTestOption { OptionText = "مشدودة", Score = 2 },
                        new SkinTestOption { OptionText = "طبيعية", Score = 3 },
                        new SkinTestOption { OptionText = "لامعة أو دهنية", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "عند التقاط الصور الفوتوغرافية، هل يبدو وجهك لامعاً؟",
                    Category = "OD",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "أبداً", Score = 1 },
                        new SkinTestOption { OptionText = "أحياناً", Score = 2 },
                        new SkinTestOption { OptionText = "غالباً", Score = 3 },
                        new SkinTestOption { OptionText = "دائماً", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "كيف يبدو الوجه أو المكياج بعد 3 ساعات من وضعه؟",
                    Category = "OD",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "متشقق أو متكتل", Score = 1 },
                        new SkinTestOption { OptionText = "طبيعي أو ناعم", Score = 2 },
                        new SkinTestOption { OptionText = "لامع", Score = 3 },
                        new SkinTestOption { OptionText = "سائل أو دهني بشكل زائد", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "ما حجم مسام الوجه عند النظر في المرآة؟",
                    Category = "OD",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "صغيرة جداً أو غير مرئية", Score = 1 },
                        new SkinTestOption { OptionText = "متوسطة", Score = 2 },
                        new SkinTestOption { OptionText = "كبيرة", Score = 3 },
                        new SkinTestOption { OptionText = "كبيرة جداً وواضحة", Score = 4 }
                    }
                },

                // ===== القسم الثاني: S/R - الحساسية مقابل المقاومة (3 أسئلة) =====
                new SkinTestQuestion
                {
                    QuestionText = "هل تعاني من ظهور بثور أو حبوب حمراء؟",
                    Category = "SR",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "أبداً", Score = 1 },
                        new SkinTestOption { OptionText = "نادراً", Score = 2 },
                        new SkinTestOption { OptionText = "مرة واحدة شهرياً على الأقل", Score = 3 },
                        new SkinTestOption { OptionText = "مرة أسبوعياً على الأقل", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "هل تسبب منتجات العناية بالبشرة حكة أو احمراراً؟",
                    Category = "SR",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "أبداً", Score = 1 },
                        new SkinTestOption { OptionText = "نادراً", Score = 2 },
                        new SkinTestOption { OptionText = "غالباً", Score = 3 },
                        new SkinTestOption { OptionText = "دائماً", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "هل تم تشخيصك سابقاً بحب الشباب أو الوردية أو الأكزيما؟",
                    Category = "SR",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "لا", Score = 1 },
                        new SkinTestOption { OptionText = "يُقال إن لدي بشرة حساسة", Score = 2 },
                        new SkinTestOption { OptionText = "نعم", Score = 3 },
                        new SkinTestOption { OptionText = "نعم، وبشكل مزمن أو شديد", Score = 4 }
                    }
                },

                // ===== القسم الثالث: P/N - التصبغ مقابل عدم التصبغ (3 أسئلة) =====
                new SkinTestQuestion
                {
                    QuestionText = "بعد اختفاء الحبوب أو الجروح، هل تترك أثراً داكناً؟",
                    Category = "PN",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "أبداً", Score = 1 },
                        new SkinTestOption { OptionText = "أحياناً", Score = 2 },
                        new SkinTestOption { OptionText = "غالباً", Score = 3 },
                        new SkinTestOption { OptionText = "دائماً", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "عدد البقع الداكنة الحالية في الوجه؟",
                    Category = "PN",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "لا يوجد", Score = 1 },
                        new SkinTestOption { OptionText = "قليلة (1-5)", Score = 2 },
                        new SkinTestOption { OptionText = "متوسطة (6-15)", Score = 3 },
                        new SkinTestOption { OptionText = "كثيرة جداً (أكثر من 16)", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "عند التعرض للشمس دون حماية، ماذا يحدث؟",
                    Category = "PN",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "احمرار فقط دون اسمرار", Score = 1 },
                        new SkinTestOption { OptionText = "احمرار ثم اسمرار", Score = 2 },
                        new SkinTestOption { OptionText = "اسمرار فقط", Score = 3 },
                        new SkinTestOption { OptionText = "اسمرار سريع ولون داكن جداً", Score = 4 }
                    }
                },

                // ===== القسم الرابع: W/T - التجاعيد مقابل المرونة (3 أسئلة) =====
                new SkinTestQuestion
                {
                    QuestionText = "هل تدخن السجائر؟",
                    Category = "WT",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "لا", Score = 1 },
                        new SkinTestOption { OptionText = "توقفت سابقاً", Score = 2 },
                        new SkinTestOption { OptionText = "نعم، بشكل محدود", Score = 3 },
                        new SkinTestOption { OptionText = "نعم، يومياً", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "ما مدى استخدامك لواقي الشمس؟",
                    Category = "WT",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "يومياً", Score = 1 },
                        new SkinTestOption { OptionText = "غالباً", Score = 2 },
                        new SkinTestOption { OptionText = "أحياناً", Score = 3 },
                        new SkinTestOption { OptionText = "أبداً", Score = 4 }
                    }
                },
                new SkinTestQuestion
                {
                    QuestionText = "ما لون بشرتك الطبيعي دون التعرض للشمس؟",
                    Category = "WT",
                    Options = new List<SkinTestOption>
                    {
                        new SkinTestOption { OptionText = "بني غامق أو أسود", Score = 1 },
                        new SkinTestOption { OptionText = "بني متوسط", Score = 2 },
                        new SkinTestOption { OptionText = "بني فاتح أو بيج", Score = 3 },
                        new SkinTestOption { OptionText = "أبيض جداً أو شاحب", Score = 4 }
                    }
                }
            };

            _context.SkinTestQuestions.AddRange(questions);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Questions seeded successfully!", count = questions.Count });
        }

        // ✅ Seed جدول الأنواع الـ16 (الوصف والاستراتيجية حسب الورقة البحثية)
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
                    Strategy = "بشرة متوازنة وقوية؛ يوصى بتنظيف لطيف وروتين بسيط للحفاظ على التوازن."
                },
                new SkinTypeProfile
                {
                    Code = "ORPT",
                    Description = "دهنية - مقاومة - مصبغة - مشدودة",
                    Strategy = "التركيز على تفتيح البقع باستخدام فيتامين C مع حماية منتظمة من الشمس."
                },
                new SkinTypeProfile
                {
                    Code = "ORPW",
                    Description = "دهنية - مقاومة - مصبغة - مجعدة",
                    Strategy = "روتين مضاد للشيخوخة يشمل واقي الشمس ومضادات أكسدة مثل الريتينول."
                },
                new SkinTypeProfile
                {
                    Code = "ORNW",
                    Description = "دهنية - مقاومة - غير مصبغة - مجعدة",
                    Strategy = "يمكنها تحمّل علاجات التجاعيد القوية مع الحفاظ على ترطيب معتدل."
                },
                new SkinTypeProfile
                {
                    Code = "OSNT",
                    Description = "دهنية - حساسة - غير مصبغة - مشدودة",
                    Strategy = "تهدئة الالتهاب أولاً باستخدام مكونات مهدئة مع التحكم في الدهون."
                },
                new SkinTypeProfile
                {
                    Code = "OSPT",
                    Description = "دهنية - حساسة - مصبغة - مشدودة",
                    Strategy = "علاج التصبغ بمواد غير مهيجة مثل حمض الأزيليك مع حماية شمسية صارمة."
                },
                new SkinTypeProfile
                {
                    Code = "OSPW",
                    Description = "دهنية - حساسة - مصبغة - مجعدة",
                    Strategy = "من أكثر الأنواع تعقيداً؛ يتطلب روتيناً دقيقاً وإشرافاً طبياً مستمراً."
                },
                new SkinTypeProfile
                {
                    Code = "OSNW",
                    Description = "دهنية - حساسة - غير مصبغة - مجعدة",
                    Strategy = "استخدام الريتينول بتركيزات منخفضة مع مكونات مضادة للالتهاب."
                },
                new SkinTypeProfile
                {
                    Code = "DRNT",
                    Description = "جافة - مقاومة - غير مصبغة - مشدودة",
                    Strategy = "دعم حاجز البشرة بترطيب عميق غني بالسيراميدات."
                },
                new SkinTypeProfile
                {
                    Code = "DRPT",
                    Description = "جافة - مقاومة - مصبغة - مشدودة",
                    Strategy = "الجمع بين الترطيب والتفتيح باستخدام أحماض الفواكه الخفيفة (AHAs)."
                },
                new SkinTypeProfile
                {
                    Code = "DRPW",
                    Description = "جافة - مقاومة - مصبغة - مجعدة",
                    Strategy = "روتين شامل: ترطيب مكثف + تفتيح + مضادات شيخوخة."
                },
                new SkinTypeProfile
                {
                    Code = "DRNW",
                    Description = "جافة - مقاومة - غير مصبغة - مجعدة",
                    Strategy = "ترطيب منتظم مع برامج مبكرة لمكافحة الشيخوخة."
                },
                new SkinTypeProfile
                {
                    Code = "DSNT",
                    Description = "جافة - حساسة - غير مصبغة - مشدودة",
                    Strategy = "بشرة شديدة التفاعل؛ تحتاج منتجات Hypoallergenic وترطيب مكثف."
                },
                new SkinTypeProfile
                {
                    Code = "DSPT",
                    Description = "جافة - حساسة - مصبغة - مشدودة",
                    Strategy = "علاج التصبغ بحذر شديد مع الحفاظ على ترطيب دائم."
                },
                new SkinTypeProfile
                {
                    Code = "DSPW",
                    Description = "جافة - حساسة - مصبغة - مجعدة",
                    Strategy = "أولوية لتقوية الحاجز الجلدي قبل البدء في علاج التجاعيد."
                },
                new SkinTypeProfile
                {
                    Code = "DSNW",
                    Description = "جافة - حساسة - غير مصبغة - مجعدة",
                    Strategy = "مكافحة التجاعيد بمواد لطيفة مثل Peptides مع ترطيب عميق."
                }
            };

            _context.SkinTypeProfiles.AddRange(profiles);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Skin type profiles seeded successfully!", count = profiles.Count });
        }
    }
}
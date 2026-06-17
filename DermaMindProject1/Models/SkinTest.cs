using System;
using System.Collections.Generic;

namespace DermaApp.API.Models
{
    public class SkinTestQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }

        // واحدة من: "OD", "SR", "PN", "WT"
        public string Category { get; set; }

        public List<SkinTestOption> Options { get; set; }
    }

    public class SkinTestOption
    {
        public int Id { get; set; }
        public string? OptionText { get; set; }
        public int QuestionId { get; set; }
        public SkinTestQuestion? Question { get; set; }

        // من 1 إلى 4 حسب مقياس ليكرت في الورقة البحثية
        public int Score { get; set; }
    }

    public class SkinTestResult
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public User? User { get; set; }

        // الكود النهائي المكوّن من 4 حروف، مثل OSPW
        public string SkinTypeCode { get; set; }

        public int OD_Score { get; set; }
        public int SR_Score { get; set; }
        public int PN_Score { get; set; }
        public int WT_Score { get; set; }

        public DateTime TakenAt { get; set; } = DateTime.UtcNow;
    }

    // جدول مرجعي ثابت لكل الأنواع الـ16 الممكنة، مأخوذ من الورقة البحثية
    public class SkinTypeProfile
    {
        public int Id { get; set; }
        public string Code { get; set; } // e.g. "OSPW"
        public string Description { get; set; }
        public string Strategy { get; set; }
    }
}
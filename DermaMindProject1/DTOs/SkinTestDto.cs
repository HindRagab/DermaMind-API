using System.Collections.Generic;
namespace DermaApp.API.DTOs
{
    public class SkinTestOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; }
        public int Score { get; set; }
    }

    public class SkinTestQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public string Category { get; set; }
        public List<SkinTestOptionDto> Options { get; set; }
    }

    public class SubmitSkinTestDto
    {
        public List<int> SelectedOptionIds { get; set; }
    }

    public class SkinTestResultDto
    {
        public string SkinTypeCode { get; set; }
        public int OD_Score { get; set; }
        public int SR_Score { get; set; }
        public int PN_Score { get; set; }
        public int WT_Score { get; set; }
        public string Description { get; set; }
        public string Strategy { get; set; }
        public DateTime TakenAt { get; set; }
    }
}
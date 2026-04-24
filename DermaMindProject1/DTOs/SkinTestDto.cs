namespace DermaApp.API.DTOs
{
    public class SkinTestOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; }
        public string SkinTypePoint { get; set; }
    }

    public class SkinTestQuestionDto
    {
        public int Id { get; set; }
        public string? QuestionText { get; set; }
        public List<SkinTestOptionDto> Options { get; set; }
    }

    public class SubmitSkinTestDto
    {
        public List<int> SelectedOptionIds { get; set; }
    }
}
namespace DermaApp.API.Models
{
    public class SkinTestQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public List<SkinTestOption> Options { get; set; }
    }

    public class SkinTestOption
    {
        public int Id { get; set; }
        public string? OptionText { get; set; }
        public int QuestionId { get; set; }
        public SkinTestQuestion? Question { get; set; }
        public string? SkinTypePoint { get; set; } // Oily, Dry, Normal, Combination
    }

    public class SkinTestResult
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public User? User { get; set; }
        public string? SkinType { get; set; }
        public DateTime TakenAt { get; set; } = DateTime.UtcNow;
    }
}
namespace DermaApp.API.Models
{
    public class DermaScanResult
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ResultJson { get; set; } = string.Empty;
        public string? Diagnosis { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
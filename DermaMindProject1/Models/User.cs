using Microsoft.AspNetCore.Identity;

namespace DermaApp.API.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public string? ProfileImage { get; set; }
        public string? SkinType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
using System.ComponentModel.DataAnnotations;
namespace DermaApp.API.DTOs
{
    public class ResetPasswordDto
    {
        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required, MinLength(6)]
        public string? NewPassword { get; set; }

        [Required, Compare("NewPassword")]
        public string? ConfirmPassword { get; set; }
    }
}
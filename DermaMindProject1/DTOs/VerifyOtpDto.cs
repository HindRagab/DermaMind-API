using System.ComponentModel.DataAnnotations;

namespace DermaApp.API.DTOs
{
    public class VerifyOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }
    }
}
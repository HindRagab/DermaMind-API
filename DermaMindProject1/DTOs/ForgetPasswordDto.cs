using System.ComponentModel.DataAnnotations;

namespace DermaApp.API.DTOs
{
    public class ForgetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}
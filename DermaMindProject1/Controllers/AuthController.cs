using DermaApp.API.DTOs;
using DermaApp.API.Models;
using DermaApp.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;

        // تخزين OTP مؤقتاً في الميموري
        private static Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();

        public AuthController(UserManager<User> userManager,
            IConfiguration config, EmailService emailService)
        {
            _userManager = userManager;
            _config = config;
            _emailService = emailService;
        }

        // ✅ Register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email already exists" });

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Registered successfully!" });
        }

        // ✅ Login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Invalid email or password" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful",
                token,
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email
                }
            });
        }

        // ✅ Forget Password - بيبعت OTP
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(new { message = "Email not found" });

            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[dto.Email] = (otp, DateTime.UtcNow.AddMinutes(10));

            await _emailService.SendOtpAsync(dto.Email, otp);

            return Ok(new { message = "OTP sent to your email" });
        }

        // ✅ Verify OTP
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp(VerifyOtpDto dto)
        {
            if (!_otpStore.ContainsKey(dto.Email))
                return BadRequest(new { message = "No OTP found for this email" });

            var (storedOtp, expiry) = _otpStore[dto.Email];

            if (DateTime.UtcNow > expiry)
            {
                _otpStore.Remove(dto.Email);
                return BadRequest(new { message = "OTP has expired" });
            }

            if (storedOtp != dto.Otp)
                return BadRequest(new { message = "Invalid OTP" });

            return Ok(new { message = "OTP verified successfully" });
        }

        // ✅ Reset Password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!_otpStore.ContainsKey(dto.Email))
                return BadRequest(new { message = "Please verify OTP first" });

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            _otpStore.Remove(dto.Email);

            return Ok(new { message = "Password reset successfully!" });
        }

        // 🔧 Helper - Generate JWT Token
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["JWT:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JWT:Issuer"],
                audience: _config["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(
                    double.Parse(_config["JWT:DurationInDays"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
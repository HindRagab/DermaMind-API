using DermaApp.API.Data;
using DermaApp.API.DTOs;
using DermaApp.API.Models;
using DermaApp.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;
        private readonly AppDbContext _context;

        public AuthController(UserManager<User> userManager,
            IConfiguration config, EmailService emailService, AppDbContext context)
        {
            _userManager = userManager;
            _config = config;
            _emailService = emailService;
            _context = context;
        }


        // ✅ Register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDto dto)
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

            // رفع الصورة لو موجودة
            if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ProfileImage.CopyToAsync(stream);
                }

                user.ProfileImage = $"/images/{fileName}";
            }

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Registered successfully!", profileImage = user.ProfileImage });
        }

        // ✅ Login
        [HttpPost("login")]
        //public async Task<IActionResult> Login(LoginDto dto)
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
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

            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // حذف أي OTP قديم لنفس الإيميل
            var oldOtps = _context.OtpEntries.Where(o => o.Email == dto.Email);
            _context.OtpEntries.RemoveRange(oldOtps);

            // حفظ OTP جديد في الـ Database
            _context.OtpEntries.Add(new OtpEntry
            {
                Email = dto.Email,
                Otp = otp,
                Expiry = DateTime.UtcNow.AddMinutes(10)
            });
            await _context.SaveChangesAsync();

            await _emailService.SendOtpAsync(dto.Email, otp);

            return Ok(new { message = "OTP sent to your email" });
        }

        // ✅ Verify OTP
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
        {
            var otpEntry = await _context.OtpEntries
                .FirstOrDefaultAsync(o => o.Email == dto.Email && !o.IsUsed);

            if (otpEntry == null)
                return BadRequest(new { message = "No OTP found for this email" });

            if (DateTime.UtcNow > otpEntry.Expiry)
            {
                _context.OtpEntries.Remove(otpEntry);
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "OTP has expired" });
            }

            if (otpEntry.Otp != dto.Otp)
                return BadRequest(new { message = "Invalid OTP" });

            // علّم الـ OTP إنه اتتحقق منه
            otpEntry.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "OTP verified successfully" });
        }

        // ✅ Reset Password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            // تأكد إن الـ OTP اتتحقق منه
            var otpEntry = await _context.OtpEntries
                .FirstOrDefaultAsync(o => o.Email == dto.Email && o.IsUsed);

            if (otpEntry == null)
                return BadRequest(new { message = "Please verify OTP first" });

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // امسح الـ OTP من الـ Database
            _context.OtpEntries.Remove(otpEntry);
            await _context.SaveChangesAsync();

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
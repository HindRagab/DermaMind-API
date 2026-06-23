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
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

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
        private readonly Cloudinary _cloudinary;

        public AuthController(UserManager<User> userManager,
            IConfiguration config, EmailService emailService, AppDbContext context)
        {
            _userManager = userManager;
            _config = config;
            _emailService = emailService;
            _context = context;

            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
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

            // رفع الصورة على Cloudinary
            if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
            {
                using var stream = dto.ProfileImage.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(dto.ProfileImage.FileName, stream),
                    Folder = "dermamind/profiles"
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult?.SecureUrl != null)
                    user.ProfileImage = uploadResult.SecureUrl.ToString();
            }

            // ✅ حفظ الـ user في الداتا بيز
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Registered successfully!", profileImage = user.ProfileImage });
        }

        // ✅ Login
        [HttpPost("login")]
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

            var oldOtps = _context.OtpEntries.Where(o => o.Email == dto.Email);
            _context.OtpEntries.RemoveRange(oldOtps);

            _context.OtpEntries.Add(new OtpEntry
            {
                Email = dto.Email,
                Otp = otp,
                Expiry = DateTime.UtcNow.AddMinutes(10)
            });
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendOtpAsync(dto.Email, otp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Email sending failed",
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }

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

            otpEntry.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "OTP verified successfully" });
        }

        // ✅ Reset Password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
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

            _context.OtpEntries.Remove(otpEntry);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully!" });
        }
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            try
            {
                var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { "81157765082-f91sd3ld0pij8btnrk36ichplpm3vhto.apps.googleusercontent.com" }
                };

                var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);

                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user == null)
                {

                    // مستخدم جديد - نعمل register تلقائي
                    user = new User
                    {
                        FullName = !string.IsNullOrWhiteSpace(payload.Name)
        ? payload.Name
        : payload.Email.Split('@')[0],
                        Email = payload.Email,
                        UserName = payload.Email,
                        ProfileImage = payload.Picture,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                        return BadRequest(result.Errors);
                }

                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    message = "Google login successful",
                    token,
                    user = new
                    {
                        user.Id,
                        user.FullName,
                        user.Email,
                        user.ProfileImage
                    }
                });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = "Invalid Google token", error = ex.Message, inner = innerMessage });
            }
        }
        [HttpGet("debug-google-config")]
        public IActionResult DebugGoogleConfig()
        {
            var hardcodedClientId = "81157765082-f91sd3ld0pij8btnrk36ichplpm3vhto.apps.googleusercontent.com";
            return Ok(new { hardcoded = hardcodedClientId });
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
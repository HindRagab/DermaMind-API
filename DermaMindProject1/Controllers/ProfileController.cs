using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DermaApp.API.Data;
using DermaApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;

        public ProfileController(UserManager<User> userManager, AppDbContext context, IConfiguration config)
        {
            _userManager = userManager;
            _context = context;

            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        // ✅ جلب بيانات المستخدم
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.ProfileImage,
                user.SkinType
            });
        }

        // ✅ تعديل الاسم والـ SkinType وصورة البروفايل
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile(
            [FromForm] string? fullName,
            [FromForm] string? skinType,
            IFormFile? image)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // تحديث الاسم
            if (!string.IsNullOrEmpty(fullName))
                user.FullName = fullName;

            // تحديث نوع البشرة
            if (!string.IsNullOrEmpty(skinType))
                user.SkinType = skinType;

            // رفع الصورة على Cloudinary لو موجودة
            if (image != null && image.Length > 0)
            {
                using var stream = image.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(image.FileName, stream),
                    Folder = "dermamind/profiles",
                    Transformation = new Transformation().Width(300).Height(300).Crop("fill")
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult?.SecureUrl != null)
                    user.ProfileImage = uploadResult.SecureUrl.ToString();
            }

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                message = "Profile updated successfully!",
                user.FullName,
                user.SkinType,
                user.ProfileImage
            });
        }
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? SkinType { get; set; }
    }
}
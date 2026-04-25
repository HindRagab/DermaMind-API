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

        public ProfileController(UserManager<User> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
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

        // ✅ تعديل بيانات المستخدم
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            user.FullName = dto.FullName ?? user.FullName;
            user.SkinType = dto.SkinType ?? user.SkinType;

            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Profile updated successfully!" });
        }

        // ✅ رفع صورة شخصية
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Please upload an image" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);

            // حفظ الصورة في فولدر wwwroot/images
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            user.ProfileImage = $"/images/{fileName}";
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                message = "Image uploaded successfully!",
                imageUrl = user.ProfileImage
            });
        }
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? SkinType { get; set; }
    }
}
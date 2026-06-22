using DermaApp.API.Data;
using DermaApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ إضافة منتج للمفضلة
        [HttpPost("add/{productId}")]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var exists = await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (exists)
                return BadRequest(new { message = "Product already in wishlist" });

            var item = new WishlistItem
            {
                UserId = userId ?? "",
                ProductId = productId
            };

            _context.WishlistItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Added to wishlist successfully!" });
        }

        // ✅ جلب المفضلة بتاعة اليوزر
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var wishlist = await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => new
                {
                    w.Id,
                    w.AddedAt,
                    Product = w.Product
                })
                .ToListAsync();

            return Ok(wishlist);
        }

        // ✅ حذف منتج من المفضلة
        [HttpDelete("remove/{wishlistItemId}")]
        public async Task<IActionResult> RemoveFromWishlist(int wishlistItemId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.Id == wishlistItemId && w.UserId == userId);

            if (item == null)
                return NotFound(new { message = "Item not found in wishlist" });

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Removed from wishlist successfully!" });
        }
    }
}
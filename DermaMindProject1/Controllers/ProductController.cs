using DermaApp.API.Data;
using DermaApp.API.Models;
using DermaApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        public ProductController(IHttpClientFactory httpClientFactory, AppDbContext context)
        {
            _httpClient = httpClientFactory.CreateClient();
            _context = context;
        }

        // ✅ جلب كل المنتجات
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        // ✅ جلب منتجات Skincare
        [HttpGet("skincare")]
        public async Task<IActionResult> GetSkincareProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        // ✅ البحث عن منتج
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
                return BadRequest(new { message = "Please enter a search term" });

            var products = await _context.Products
                .Where(p => p.Name.Contains(query) ||
                            p.Brand.Contains(query) ||
                            p.Category.Contains(query))
                .ToListAsync();

            return Ok(products);
        }
        [HttpDelete("all")]
        [Authorize]
        public async Task<IActionResult> DeleteAllProducts()
        {
            var all = await _context.Products.ToListAsync();
            _context.Products.RemoveRange(all);
            await _context.SaveChangesAsync();
            return Ok(new { message = "All products deleted!" });
        }
        // ✅ إضافة منتج (Admin)
        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddProduct(
    [FromForm] string name,
    [FromForm] string brand,
    [FromForm] string description,
    [FromForm] decimal price,
    [FromForm] string category,
    IFormFile? image,
    [FromServices] CloudinaryService cloudinary)
        {
            string imageUrl = "";

            if (image != null && image.Length > 0)
                imageUrl = await cloudinary.UploadImageAsync(image);

            var product = new Product
            {
                Name = name,
                Brand = brand,
                Description = description,
                Price = price,
                Category = category,
                ImageUrl = imageUrl
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product added successfully!", product });
        }

        

    }
}
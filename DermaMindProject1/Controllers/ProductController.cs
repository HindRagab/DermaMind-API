using DermaApp.API.Data;
using DermaApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        // ✅ جلب منتجات Skincare من Makeup API
        [HttpGet("skincare")]
        public async Task<IActionResult> GetSkincareProducts()
        {
            try
            {
                var url = "https://makeup-api.herokuapp.com/api/v1/products.json?product_type=skincare";
                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                var products = new List<object>();
                foreach (var product in data.EnumerateArray())
                {
                    var name = product.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var brand = product.TryGetProperty("brand", out var b) ? b.GetString() : null;
                    var price = product.TryGetProperty("price", out var p) ? p.GetString() : null;
                    var image = product.TryGetProperty("image_link", out var img) ? img.GetString() : null;

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(image))
                    {
                        products.Add(new
                        {
                            name,
                            brand = brand ?? "Unknown",
                            price = price ?? "0",
                            image
                        });
                    }
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching products", error = ex.Message });
            }
        }

        // ✅ البحث عن منتج
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
                return BadRequest(new { message = "Please enter a search term" });

            try
            {
                var url = $"https://makeup-api.herokuapp.com/api/v1/products.json?brand={query}";
                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                var products = new List<object>();
                foreach (var product in data.EnumerateArray())
                {
                    var name = product.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var brand = product.TryGetProperty("brand", out var b) ? b.GetString() : null;
                    var price = product.TryGetProperty("price", out var p) ? p.GetString() : null;
                    var image = product.TryGetProperty("image_link", out var img) ? img.GetString() : null;

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(image))
                    {
                        products.Add(new
                        {
                            name,
                            brand = brand ?? "Unknown",
                            price = price ?? "0",
                            image
                        });
                    }
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching products", error = ex.Message });
            }
        }

        // ✅ جلب منتجات من الـ Database
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        // ✅ إضافة منتج (Admin)
        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product added successfully!", product });
        }
    }
}
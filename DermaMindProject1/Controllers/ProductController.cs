using DermaApp.API.Data;
using DermaApp.API.Models;
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

        // ✅ جلب منتجات من الـ Database
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        // ✅ إضافة منتج جديد
        [HttpPost("add")]
        public async Task<IActionResult> AddProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product added successfully!", product });
        }

        // ✅ جلب منتجات الـ Skincare من Open Beauty Facts
        [HttpGet("skincare")]
        public async Task<IActionResult> GetSkincareProducts()
        {
            var url = "https://world.openbeautyfacts.org/cgi/search.pl?search_terms=skincare&search_simple=1&action=process&json=1&page_size=20";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var products = new List<object>();
            if (data.TryGetProperty("products", out var productList))
            {
                foreach (var product in productList.EnumerateArray())
                {
                    var name = product.TryGetProperty("product_name", out var n) ? n.GetString() : "Unknown";
                    var image = product.TryGetProperty("image_url", out var img) ? img.GetString() : "";
                    var brand = product.TryGetProperty("brands", out var b) ? b.GetString() : "";
                    var category = product.TryGetProperty("categories", out var c) ? c.GetString() : "";

                    if (!string.IsNullOrEmpty(name) && name != "Unknown")
                    {
                        products.Add(new { name, brand, image, category });
                    }
                }
            }
            return Ok(products);
        }

        // ✅ البحث عن منتج
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
                return BadRequest(new { message = "Please enter a search term" });

            var url = $"https://world.openbeautyfacts.org/cgi/search.pl?search_terms={query}&search_simple=1&action=process&json=1&page_size=10";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var products = new List<object>();
            if (data.TryGetProperty("products", out var productList))
            {
                foreach (var product in productList.EnumerateArray())
                {
                    var name = product.TryGetProperty("product_name", out var n) ? n.GetString() : "Unknown";
                    var image = product.TryGetProperty("image_url", out var img) ? img.GetString() : "";
                    var brand = product.TryGetProperty("brands", out var b) ? b.GetString() : "";
                    var category = product.TryGetProperty("categories", out var c) ? c.GetString() : "";

                    if (!string.IsNullOrEmpty(name) && name != "Unknown")
                    {
                        products.Add(new { name, brand, image, category });
                    }
                }
            }
            return Ok(products);
        }
    }
}
using DermaApp.API.Data;
using DermaApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _paymobApiKey;
        private readonly int _integrationId;

        public CartController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _paymobApiKey = config["Paymob:ApiKey"]!;
            _integrationId = config.GetValue<int>("Paymob:IntegrationId");
        }

        // ✅ إضافة منتج للسلة
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Product added to cart!" });
        }

        // ✅ جلب السلة
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    c.Id,
                    c.Quantity,
                    Product = new
                    {
                        c.Product.Id,
                        c.Product.Name,
                        c.Product.Price,
                        c.Product.ImageUrl
                    },
                    Total = c.Quantity * c.Product.Price
                }).ToListAsync();

            var grandTotal = cartItems.Sum(c => c.Total);
            return Ok(new { cartItems, grandTotal });
        }

        // ✅ حذف منتج من السلة
        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (item == null)
                return NotFound(new { message = "Item not found" });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Item removed from cart!" });
        }

        // ✅ Checkout - الدفع عن طريق Paymob
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(userId);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest(new { message = "Cart is empty" });

            var totalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price);

            // Step 1: Auth Token
            var authResponse = await _httpClient.PostAsync(
                "https://accept.paymob.com/api/auth/tokens",
                new StringContent(
                    JsonSerializer.Serialize(new { api_key = _paymobApiKey }),
                    Encoding.UTF8, "application/json"));

            var authJson = await authResponse.Content.ReadAsStringAsync();
            var authData = JsonSerializer.Deserialize<JsonElement>(authJson);
            var authToken = authData.GetProperty("token").GetString();

            // Step 2: Create Order
            var orderItems = cartItems.Select(c => new
            {
                name = c.Product.Name,
                amount_cents = (int)(c.Product.Price * 100),
                description = c.Product.Description ?? "",
                quantity = c.Quantity
            }).ToList();

            var orderResponse = await _httpClient.PostAsync(
                "https://accept.paymob.com/api/ecommerce/orders",
                new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        auth_token = authToken,
                        delivery_needed = false,
                        amount_cents = (int)(totalAmount * 100),
                        currency = "EGP",
                        items = orderItems
                    }),
                    Encoding.UTF8, "application/json"));

            var orderJson = await orderResponse.Content.ReadAsStringAsync();
            var orderData = JsonSerializer.Deserialize<JsonElement>(orderJson);
            var paymobOrderId = orderData.GetProperty("id").GetInt64();

            // Step 3: Payment Key
            var paymentKeyResponse = await _httpClient.PostAsync(
                "https://accept.paymob.com/api/acceptance/payment_keys",
                new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        auth_token = authToken,
                        amount_cents = (int)(totalAmount * 100),
                        expiration = 3600,
                        order_id = paymobOrderId,
                        billing_data = new
                        {
                            first_name = user.FullName ?? "Customer",
                            last_name = ".",
                            email = user.Email,
                            phone_number = "01000000000",
                            apartment = "NA",
                            floor = "NA",
                            street = "NA",
                            building = "NA",
                            shipping_method = "NA",
                            postal_code = "NA",
                            city = "NA",
                            country = "EG",
                            state = "NA"
                        },
                        currency = "EGP",
                        integration_id = _integrationId
                    }),
                    Encoding.UTF8, "application/json"));

            var paymentKeyJson = await paymentKeyResponse.Content.ReadAsStringAsync();
            var paymentKeyData = JsonSerializer.Deserialize<JsonElement>(paymentKeyJson);
            var paymentKey = paymentKeyData.GetProperty("token").GetString();

            // Save Order in DB
            var order = new Order
            {
                UserId = userId,
                TotalAmount = totalAmount,
                PaymobOrderId = paymobOrderId.ToString(),
                Status = "Pending",
                Items = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    Price = c.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            var paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_integrationId}?payment_token={paymentKey}";

            return Ok(new
            {
                message = "Proceed to payment",
                paymentUrl,
                orderId = order.Id,
                totalAmount
            });
        }
    }
}
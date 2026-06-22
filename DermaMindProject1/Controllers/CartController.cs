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
        private readonly string _paymobSecretKey = "egy_sk_test_10b383beab01aa46afc6ca37d04b3738a681ecdad9ea4c4eafeb0318507825e7";
        private readonly string _paymobPublicKey = "egy_pk_test_EKfHUUk1hx9RHrpOCTcu2s5BDP8udJwD";
        private readonly int _integrationId = 5634242;

        public CartController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_paymobSecretKey}");
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
                existingItem.Quantity += quantity;
            else
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });

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

        // ✅ Checkout
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

            var orderItems = cartItems.Select(c => new
            {
                name = c.Product.Name,
                amount = (int)(c.Product.Price * 100),
                description = c.Product.Description ?? "",
                quantity = c.Quantity
            }).ToList();

            try
            {
                var intentionResponse = await _httpClient.PostAsync(
                    "https://accept.paymob.com/v1/intention/",
                    new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            amount = (int)(totalAmount * 100),
                            currency = "EGP",
                            payment_methods = new[] { _integrationId },
                            items = orderItems,
                            billing_data = new
                            {
                                first_name = user.FullName ?? "Customer",
                                last_name = ".",
                                email = user.Email,
                                phone_number = user.PhoneNumber ?? "01000000000"
                            }
                        }),
                        Encoding.UTF8, "application/json"));

                var intentionJson = await intentionResponse.Content.ReadAsStringAsync();

                if (!intentionResponse.IsSuccessStatusCode)
                    return StatusCode(500, new { step = "intention", paymobResponse = intentionJson });

                var intentionData = JsonSerializer.Deserialize<JsonElement>(intentionJson);
                var clientSecret = intentionData.GetProperty("client_secret").GetString();

                // Save Order
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    PaymobOrderId = "", // ← ضيفي ده
                    Items = cartItems.Select(c => new OrderItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        Price = c.Product.Price
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                var paymentUrl = $"https://accept.paymob.com/unifiedcheckout/?publicKey={_paymobPublicKey}&clientSecret={clientSecret}";

                return Ok(new
                {
                    message = "Proceed to payment",
                    paymentUrl,
                    orderId = order.Id,
                    totalAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Checkout failed",
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        // ✅ Webhook
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymobWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(body);

            try
            {
                var obj = data.GetProperty("obj");
                var success = obj.GetProperty("success").GetBoolean();
                var paymobOrderId = obj.GetProperty("order").GetProperty("id").GetInt64().ToString();

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.PaymobOrderId == paymobOrderId);

                if (order == null) return Ok();

                if (success && order.Status == "Pending")
                {
                    order.Status = "Paid";
                    var cartItems = await _context.CartItems
                        .Where(c => c.UserId == order.UserId)
                        .ToListAsync();
                    _context.CartItems.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();
                }
                else if (!success && order.Status == "Pending")
                {
                    order.Status = "Failed";
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            return Ok();
        }
    }
}
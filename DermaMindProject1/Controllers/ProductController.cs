using DermaApp.API.Data;
using DermaApp.API.Models;
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

        // ✅ إضافة منتج (Admin)
        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product added successfully!", product });
        }

        // ✅ Seed 60 منتج
        [HttpPost("seed")]
        public async Task<IActionResult> SeedProducts()
        {
            if (await _context.Products.AnyAsync())
                return BadRequest(new { message = "Products already exist!" });

            var products = new List<Product>
            {
                // Moisturizers
                new Product { Name = "CeraVe Moisturizing Cream", Brand = "CeraVe", Price = 450, Description = "مرطب للبشرة الجافة بالسيراميد", ImageUrl = "https://i.imgur.com/CeraVeMC.jpg", Category = "Moisturizer" },
                new Product { Name = "Neutrogena Hydro Boost Gel", Brand = "Neutrogena", Price = 380, Description = "جل مرطب بالهيالورونيك", ImageUrl = "https://i.imgur.com/HydroBoost.jpg", Category = "Moisturizer" },
                new Product { Name = "Cetaphil Moisturizing Cream", Brand = "Cetaphil", Price = 290, Description = "مرطب طبي للبشرة الحساسة", ImageUrl = "https://i.imgur.com/Cetaphil.jpg", Category = "Moisturizer" },
                new Product { Name = "La Roche-Posay Toleriane", Brand = "La Roche-Posay", Price = 520, Description = "مرطب للبشرة الحساسة", ImageUrl = "https://i.imgur.com/LaRoche.jpg", Category = "Moisturizer" },
                new Product { Name = "Nivea Soft Cream", Brand = "Nivea", Price = 120, Description = "كريم مرطب يومي خفيف", ImageUrl = "https://i.imgur.com/NiveaSoft.jpg", Category = "Moisturizer" },
                new Product { Name = "Eucerin Original Cream", Brand = "Eucerin", Price = 320, Description = "كريم علاجي للبشرة الجافة", ImageUrl = "https://i.imgur.com/Eucerin.jpg", Category = "Moisturizer" },
                new Product { Name = "Bioderma Sensibio Cream", Brand = "Bioderma", Price = 480, Description = "مرطب للبشرة الحساسة", ImageUrl = "https://i.imgur.com/Bioderma.jpg", Category = "Moisturizer" },
                new Product { Name = "Vichy Aqualia Thermal", Brand = "Vichy", Price = 560, Description = "مرطب بالمياه الحرارية", ImageUrl = "https://i.imgur.com/Vichy.jpg", Category = "Moisturizer" },
                new Product { Name = "Aveeno Daily Moisturizer", Brand = "Aveeno", Price = 350, Description = "مرطب يومي بالشوفان", ImageUrl = "https://i.imgur.com/Aveeno.jpg", Category = "Moisturizer" },
                new Product { Name = "Simple Kind to Skin Moisturizer", Brand = "Simple", Price = 180, Description = "مرطب بدون عطور وألوان", ImageUrl = "https://i.imgur.com/Simple.jpg", Category = "Moisturizer" },

                // Cleansers
                new Product { Name = "CeraVe Hydrating Cleanser", Brand = "CeraVe", Price = 280, Description = "غسول مرطب للبشرة الجافة", ImageUrl = "https://i.imgur.com/CeraVeC.jpg", Category = "Cleanser" },
                new Product { Name = "La Roche-Posay Toleriane Cleanser", Brand = "La Roche-Posay", Price = 340, Description = "غسول للبشرة الحساسة", ImageUrl = "https://i.imgur.com/LaRocheC.jpg", Category = "Cleanser" },
                new Product { Name = "Neutrogena Ultra Gentle Cleanser", Brand = "Neutrogena", Price = 220, Description = "غسول خفيف للبشرة الحساسة", ImageUrl = "https://i.imgur.com/NeutrogenaC.jpg", Category = "Cleanser" },
                new Product { Name = "Bioderma Micellar Water 500ml", Brand = "Bioderma", Price = 320, Description = "ماء ميسيلار لإزالة المكياج", ImageUrl = "https://i.imgur.com/BiodermaMW.jpg", Category = "Cleanser" },
                new Product { Name = "Cetaphil Gentle Cleanser", Brand = "Cetaphil", Price = 195, Description = "غسول لطيف لجميع أنواع البشرة", ImageUrl = "https://i.imgur.com/CetaphilC.jpg", Category = "Cleanser" },
                new Product { Name = "The Ordinary Squalane Cleanser", Brand = "The Ordinary", Price = 260, Description = "غسول بالسكوالين يرطب البشرة", ImageUrl = "https://i.imgur.com/TOSqualane.jpg", Category = "Cleanser" },
                new Product { Name = "Garnier Micellar Water", Brand = "Garnier", Price = 150, Description = "ماء ميسيلار في خطوة واحدة", ImageUrl = "https://i.imgur.com/GarnierMW.jpg", Category = "Cleanser" },
                new Product { Name = "Simple Micellar Water", Brand = "Simple", Price = 165, Description = "ماء تنظيف للبشرة الحساسة", ImageUrl = "https://i.imgur.com/SimpleMW.jpg", Category = "Cleanser" },
                new Product { Name = "Vichy Purete Thermale Cleanser", Brand = "Vichy", Price = 310, Description = "غسول بالمياه الحرارية", ImageUrl = "https://i.imgur.com/VichyC.jpg", Category = "Cleanser" },
                new Product { Name = "Eucerin DermoPure Cleanser", Brand = "Eucerin", Price = 290, Description = "غسول للبشرة الدهنية والمختلطة", ImageUrl = "https://i.imgur.com/EucerinC.jpg", Category = "Cleanser" },

                // Serums
                new Product { Name = "The Ordinary Niacinamide 10%", Brand = "The Ordinary", Price = 220, Description = "سيروم لتضييق المسام وتوحيد البشرة", ImageUrl = "https://i.imgur.com/TONiacin.jpg", Category = "Serum" },
                new Product { Name = "The Ordinary Vitamin C 23%", Brand = "The Ordinary", Price = 260, Description = "سيروم فيتامين C للإشراقة", ImageUrl = "https://i.imgur.com/TOVitC.jpg", Category = "Serum" },
                new Product { Name = "The Ordinary Hyaluronic Acid 2%", Brand = "The Ordinary", Price = 180, Description = "سيروم للترطيب العميق", ImageUrl = "https://i.imgur.com/TOHA.jpg", Category = "Serum" },
                new Product { Name = "The Ordinary Retinol 0.5%", Brand = "The Ordinary", Price = 240, Description = "سيروم لمكافحة الشيخوخة", ImageUrl = "https://i.imgur.com/TORetinol.jpg", Category = "Serum" },
                new Product { Name = "Vichy Mineral 89 Serum", Brand = "Vichy", Price = 520, Description = "سيروم مقوي بالمعادن", ImageUrl = "https://i.imgur.com/VichyS.jpg", Category = "Serum" },
                new Product { Name = "L'Oreal Revitalift Hyaluronic", Brand = "L'Oreal", Price = 420, Description = "سيروم لملء التجاعيد", ImageUrl = "https://i.imgur.com/LorealS.jpg", Category = "Serum" },
                new Product { Name = "Neutrogena Rapid Tone Repair", Brand = "Neutrogena", Price = 380, Description = "سيروم لتوحيد لون البشرة", ImageUrl = "https://i.imgur.com/NeutrogenaS.jpg", Category = "Serum" },
                new Product { Name = "CeraVe Resurfacing Retinol Serum", Brand = "CeraVe", Price = 460, Description = "سيروم الريتينول بالسيراميد", ImageUrl = "https://i.imgur.com/CeraVeS.jpg", Category = "Serum" },
                new Product { Name = "La Roche-Posay Vitamin C Serum", Brand = "La Roche-Posay", Price = 580, Description = "سيروم فيتامين C للإشراقة", ImageUrl = "https://i.imgur.com/LaRocheS.jpg", Category = "Serum" },
                new Product { Name = "Eucerin Hyaluron-Filler Serum", Brand = "Eucerin", Price = 490, Description = "سيروم لملء التجاعيد العميقة", ImageUrl = "https://i.imgur.com/EucerinS.jpg", Category = "Serum" },

                // Sunscreen
                new Product { Name = "Eucerin Sun Fluid SPF50+", Brand = "Eucerin", Price = 350, Description = "واقي شمس خفيف للاستخدام اليومي", ImageUrl = "https://i.imgur.com/EucerinSun.jpg", Category = "Sunscreen" },
                new Product { Name = "La Roche-Posay Anthelios SPF50+", Brand = "La Roche-Posay", Price = 580, Description = "واقي شمس طبي للبشرة الحساسة", ImageUrl = "https://i.imgur.com/LaRocheSun.jpg", Category = "Sunscreen" },
                new Product { Name = "Neutrogena Ultra Sheer SPF55", Brand = "Neutrogena", Price = 280, Description = "واقي شمس خفيف جداً", ImageUrl = "https://i.imgur.com/NeutrogenaS2.jpg", Category = "Sunscreen" },
                new Product { Name = "Bioderma Photoderm SPF50+", Brand = "Bioderma", Price = 480, Description = "واقي شمس للبشرة الحساسة", ImageUrl = "https://i.imgur.com/BiodermaSun.jpg", Category = "Sunscreen" },
                new Product { Name = "Vichy Capital Soleil SPF50", Brand = "Vichy", Price = 420, Description = "واقي شمس بمضادات الأكسدة", ImageUrl = "https://i.imgur.com/VichySun.jpg", Category = "Sunscreen" },
                new Product { Name = "Cetaphil Sun SPF50+ Light Gel", Brand = "Cetaphil", Price = 360, Description = "واقي شمس جيلي للبشرة الدهنية", ImageUrl = "https://i.imgur.com/CetaphilSun.jpg", Category = "Sunscreen" },

                // Toners
                new Product { Name = "Paula's Choice 2% BHA Toner", Brand = "Paula's Choice", Price = 650, Description = "تونر بالسالسيليك لتنظيف المسام", ImageUrl = "https://i.imgur.com/PaulasT.jpg", Category = "Toner" },
                new Product { Name = "Thayers Witch Hazel Toner", Brand = "Thayers", Price = 290, Description = "تونر طبيعي لتضييق المسام", ImageUrl = "https://i.imgur.com/ThayersT.jpg", Category = "Toner" },
                new Product { Name = "Some By Mi AHA BHA PHA Toner", Brand = "Some By Mi", Price = 340, Description = "تونر بالأحماض الثلاثة", ImageUrl = "https://i.imgur.com/SomeByMiT.jpg", Category = "Toner" },
                new Product { Name = "La Roche-Posay Serozinc Toner", Brand = "La Roche-Posay", Price = 310, Description = "تونر بالزنك للبشرة الدهنية", ImageUrl = "https://i.imgur.com/LaRocheT.jpg", Category = "Toner" },
                new Product { Name = "Neutrogena Alcohol-Free Toner", Brand = "Neutrogena", Price = 195, Description = "تونر بدون كحول للبشرة الحساسة", ImageUrl = "https://i.imgur.com/NeutrogenaT.jpg", Category = "Toner" },

                // Masks
                new Product { Name = "The Ordinary AHA 30% Mask", Brand = "The Ordinary", Price = 380, Description = "قناع تقشيري بالأحماض", ImageUrl = "https://i.imgur.com/TOAHA.jpg", Category = "Mask" },
                new Product { Name = "L'Oreal Pure Clay Mask", Brand = "L'Oreal", Price = 220, Description = "قناع الطين لامتصاص الدهون", ImageUrl = "https://i.imgur.com/LorealM.jpg", Category = "Mask" },
                new Product { Name = "Garnier Green Tea Mask", Brand = "Garnier", Price = 145, Description = "قناع الشاي الأخضر لتنقية البشرة", ImageUrl = "https://i.imgur.com/GarnierM.jpg", Category = "Mask" },
                new Product { Name = "Neutrogena Deep Clean Clay Mask", Brand = "Neutrogena", Price = 240, Description = "قناع طين لتنظيف المسام", ImageUrl = "https://i.imgur.com/NeutrogenaM.jpg", Category = "Mask" },

                // Eye Care
                new Product { Name = "CeraVe Eye Repair Cream", Brand = "CeraVe", Price = 380, Description = "كريم العين لتقليل الهالات", ImageUrl = "https://i.imgur.com/CeraVeE.jpg", Category = "Eye Care" },
                new Product { Name = "Neutrogena Rapid Wrinkle Eye Cream", Brand = "Neutrogena", Price = 420, Description = "كريم العين بالريتينول", ImageUrl = "https://i.imgur.com/NeutrogenaE.jpg", Category = "Eye Care" },
                new Product { Name = "La Roche-Posay Redermic Eye", Brand = "La Roche-Posay", Price = 560, Description = "كريم العين المضاد للشيخوخة", ImageUrl = "https://i.imgur.com/LaRocheE.jpg", Category = "Eye Care" },

                // Acne Treatment
                new Product { Name = "La Roche-Posay Effaclar Duo", Brand = "La Roche-Posay", Price = 480, Description = "علاج الحبوب للبشرة الحساسة", ImageUrl = "https://i.imgur.com/LaRocheAcne.jpg", Category = "Acne Treatment" },
                new Product { Name = "The Ordinary Salicylic Acid 2%", Brand = "The Ordinary", Price = 210, Description = "سيروم علاج الحبوب", ImageUrl = "https://i.imgur.com/TOSalic.jpg", Category = "Acne Treatment" },
                new Product { Name = "Vichy Normaderm Anti-Blemish", Brand = "Vichy", Price = 490, Description = "سيروم علاج البثور والمسام", ImageUrl = "https://i.imgur.com/VichyAcne.jpg", Category = "Acne Treatment" },
                new Product { Name = "Neutrogena On-The-Spot Acne", Brand = "Neutrogena", Price = 195, Description = "علاج موضعي للحبوب", ImageUrl = "https://i.imgur.com/NeutrogenaAcne.jpg", Category = "Acne Treatment" },

                // Body Care
                new Product { Name = "CeraVe SA Smoothing Body Cream", Brand = "CeraVe", Price = 340, Description = "كريم الجسم للبشرة الخشنة", ImageUrl = "https://i.imgur.com/CeraVeB.jpg", Category = "Body Care" },
                new Product { Name = "Eucerin Intensive Body Lotion", Brand = "Eucerin", Price = 280, Description = "لوشن مكثف للبشرة الجافة جداً", ImageUrl = "https://i.imgur.com/EucerinB.jpg", Category = "Body Care" },
                new Product { Name = "Nivea Body Milk Nourishing", Brand = "Nivea", Price = 165, Description = "حليب الجسم المغذي بزيت اللوز", ImageUrl = "https://i.imgur.com/NiveaB.jpg", Category = "Body Care" },
                new Product { Name = "Aveeno Skin Relief Body Wash", Brand = "Aveeno", Price = 290, Description = "غسول الجسم بالشوفان", ImageUrl = "https://i.imgur.com/AveenoB.jpg", Category = "Body Care" },
            };

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Products seeded successfully!", count = products.Count });
        }
    }
}
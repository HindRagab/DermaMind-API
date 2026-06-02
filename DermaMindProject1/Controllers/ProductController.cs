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

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        [HttpGet("skincare")]
        public async Task<IActionResult> GetSkincareProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

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

        [HttpPost("seed")]
        public async Task<IActionResult> SeedProducts()
        {
            if (await _context.Products.AnyAsync())
                return BadRequest(new { message = "Products already exist!" });

            var products = new List<Product>
            {
                new Product { Name = "iS CLINICAL Active Serum", Brand = "iS CLINICAL", Price = 1050, Description = "سيروم لتوحيد لون البشرة ومكافحة حب الشباب", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355247/iS_CLINICAL_Active_Serum_-_Helps_visibly_even_skin_tone_Excellent_for_acne-prone_skin_Anti-Aging_Face_Serum_kt2i62.jpg", Category = "Serum" },
                new Product { Name = "e.l.f. SKIN Holy Hydration Kit", Brand = "e.l.f.", Price = 820, Description = "مجموعة ترطيب كاملة للبشرة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355246/e.l.f._SKIN_Holy_Hydration_Hydrated_Ever_After_Skincare_Mini_Kit_Cleanser_Makeup_Remover_Moisturizer_Eye_ek4ndu.jpg", Category = "Moisturizer" },
                new Product { Name = "TOSOWOONG Pink Peptide 12 PDRN Serum", Brand = "TOSOWOONG", Price = 1080, Description = "سيروم بالببتيد والـ DNA السالمون للإشراقة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355257/TOSOWOONG_Pink_Peptide_12_PDRN_Serum_With_Salmon_DNA_PDRN_10_320ppm_12_Peptides_Niacinamide_Skin_glow_Hydrating_Moisturizing_o580kj.jpg", Category = "Serum" },
                new Product { Name = "TOSOWOONG Copper Peptide Face Serum", Brand = "TOSOWOONG", Price = 999, Description = "سيروم ببتيد النحاس لمكافحة الشيخوخة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355257/TOSOWOONG_Copper_Peptide_Face_Serum_Anti-Aging_with_Copper_Tripeptide_GHK-Cu_12_Multi-Peptide_Formula_for_Fine_Lines_Firming_Skin_Elasticity_Hydrating_Korean_Skincare_33ml_ekmuab.jpg", Category = "Serum" },
                new Product { Name = "TOSOWOONG Copper Peptide 12 Cream", Brand = "TOSOWOONG", Price = 12990, Description = "كريم ببتيد النحاس لشد البشرة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355257/TOSOWOONG_Copper_Peptide_12_Cream_99_Purity_of_Copper_Tripeptide-1_GHK-Cu_12_Multi-Peptide_Powerful_Anti_Aging_Formula_for_Fine_Lines_wdgzgq.jpg", Category = "Moisturizer" },
                new Product { Name = "TIRTIR Ceramic Cream Light", Brand = "TIRTIR", Price = 380, Description = "كريم خفيف بالسيراميد للترطيب اليومي", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355256/TIRTIR_Ceramic_Cream_Light_ckkac9.webp", Category = "Moisturizer" },
                new Product { Name = "Sky Sol Lip Balm SPF 25", Brand = "Sky Sol", Price = 550, Description = "بالم شفاه مع حماية من الشمس SPF25", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355255/Sky_Sol_Lip_Balm_SPF_25_3_Pack_Natural_Hydrating_Lip_Jelly_with_Broad-Spectrum_Sun_ehsjtt.webp", Category = "Lip Care" },
                new Product { Name = "SVR Sebiaclear Foaming Gel Cleanser", Brand = "SVR", Price = 1290, Description = "غسول رغوي بالسالسيليك أسيد للبشرة الدهنية", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355256/SVR_-_Sebiaclear_Foaming_Gel_-_Face_Body_Cleanser_with_Salicylic_Acid_PHA_-_For_Teenagers_to_Adults_Combination_Oily_and_Sensitive_Skin_qzpuni.jpg", Category = "Cleanser" },
                new Product { Name = "SPF 100 Face Body Sunscreen", Brand = "AA", Price = 350, Description = "واقي شمس SPF100 للوجه والجسم", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355255/SPF_100_Face_Body_Sunscreen_Broad_Spectrum_Protection_with_Antioxidants_Oil-Free_Non-Greasy_Sun_Screen_Travel_Size_Sunblock_Lightweight_Daily_Moisturizer_for_Sun_Sensitive_Skin_2_Pack_AA_qfyq9p.jpg", Category = "Sunscreen" },
                new Product { Name = "SKIN1004 Madagascar Centella Gel Cream", Brand = "SKIN1004", Price = 1020, Description = "جيل كريم بالسنتيلا لتضييق المسام", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355255/SKIN1004_Madagascar_Centella_Poremizing_Light_Gel_Cream_2.53_eyyhkm.webp", Category = "Moisturizer" },
                new Product { Name = "Real Barrier Extreme Cream Light", Brand = "Real Barrier", Price = 950, Description = "مرطب خفيف للبشرة الحساسة والجافة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355255/Real_Barrier_Extreme_Cream_Light_Lightweight_Daily_Soft_Face_Moisturizer_Facial_Moisturizing_fr6lzp.webp", Category = "Moisturizer" },
                new Product { Name = "Pure Vitamin C Serum Korean", Brand = "Korean Skincare", Price = 1800, Description = "سيروم فيتامين C كوري بدون أكسدة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355254/Pure_Vitamin_C_Serum_-_Korean_Skincare_with_Vitamin_10.5_Waterless_No_oxidation_formula_wn08ow.webp", Category = "Serum" },
                new Product { Name = "Paula's Choice Skin Balancing Toner", Brand = "Paula's Choice", Price = 1050, Description = "تونر بالنياسيناميد للبشرة الدهنية والمختلطة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355254/Paula_s_Choice_SKIN_BALANCING_Pore-Reducing_Face_Toner_with_Niacinamide_for_Oily_Skin_Combination_Minimizes_Large_Pores_Controls_Oil_Shine_Hydrates_Replenishes_Fragr_oxgpit.jpg", Category = "Toner" },
                new Product { Name = "Paula's Choice Foaming Facial Cleanser", Brand = "Paula's Choice", Price = 990, Description = "غسول رغوي بالهيالورونيك أسيد والألوة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355254/Paula_s_Choice_RESIST_Perfectly_Balanced_Foaming_Facial_Cleanser_Face_Cleanser_with_Hyaluronic_Acid_Aloe_Anti-Aging_Face_Wash_Large_Pores_Oily_Skin_Fragrance_gpudov.jpg", Category = "Cleanser" },
                new Product { Name = "Paula's Choice 20% Niacinamide Serum", Brand = "Paula's Choice", Price = 899, Description = "سيروم نياسيناميد مركّز لتوحيد البشرة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355254/Paula_s_Choice_CLINICAL_20_Niacinamide_Vitamin_B3_Concentrated_Face_Serum_Anti-Aging_Serum_for_Face_Treatment_for_Discoloration_Minimizing_wosjpp.jpg", Category = "Serum" },
                new Product { Name = "Paula's Choice 2% Salicylic Acid", Brand = "Paula's Choice", Price = 1099, Description = "محلول تقشير بالسالسيليك لعلاج حب الشباب", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355253/Paula_s_Choice_CLEAR_Anti-Redness_Exfoliating_Solution_2_Salicylic_Acid_Liquid_Exfoliant_for_Face_Face_Exfoliant_for_Mild_to_Severe_Acne_Breakouts_hyf5lp.jpg", Category = "Acne Treatment" },
                new Product { Name = "Paula's Choice C5 Vitamin C Moisturizer", Brand = "Paula's Choice", Price = 460, Description = "مرطب بفيتامين C لإشراقة البشرة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355253/Paula_s_Choice_C5_Super_Boost_Face_Moisturizer_with_5_Vitamin_C_Squalane_Daily_Face_Lotion_for_Discoloration_rirden.jpg", Category = "Moisturizer" },
                new Product { Name = "Paula's Choice 10% Azelaic Acid Booster", Brand = "Paula's Choice", Price = 1200, Description = "سيروم بحمض الأزيليك لتقليل الاحمرار", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355253/Paula_s_Choice_BOOST_10_Azelaic_Acid_Booster_Cream_Gel_Face_Serum_with_Salicylic_Acid_Minimizes_Look_of_Redness_Fades_Discoloration_Oil-Free_Skin_Brightening_Serum_for_Face_Fragrance-Free_y3ewiw.jpg", Category = "Serum" },
                new Product { Name = "PURITO Retinol & Retinal Serum", Brand = "PURITO", Price = 1700, Description = "سيروم ريتينول وريتينال لمكافحة الشيخوخة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355254/PURITO_Retinol_0.1_Retinal_0.1_Anti-Aging_Facial_Serum_for_Wrinkles_Fine_Lines_Firmer_Skin_dqlx5w.webp", Category = "Serum" },
                new Product { Name = "Organic Korean Sunscreen SPF50", Brand = "Korean Skincare", Price = 1999, Description = "واقي شمس كوري عضوي SPF50", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355252/Organic_Korean_sunscreen_SPF50_jfh6na.jpg", Category = "Sunscreen" },
                new Product { Name = "Oneskin LIP SPF 15 Mineral Sunscreen", Brand = "Oneskin", Price = 1200, Description = "واقي شمس معدني للشفاه بالببتيد", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355252/Oneskin_LIP_SPF_OS-01_Peptide_Broad_Spectrum_SPF_15_Mineral_Sunscreen-_Scientifically_Proven_to_a8bpko.webp", Category = "Lip Care" },
                new Product { Name = "Olay Super Serum Glow", Brand = "Olay", Price = 340, Description = "سيروم إشراقة بالنياسيناميد وفيتامين C", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355252/Olay_Super_Serum_-_Glow_Serum_for_Face_-_Activated_Niacinamide_Vitamin_C_E_Collagen_Peptide_pvki3z.webp", Category = "Serum" },
                new Product { Name = "Olay Regenerist Micro-Sculpting Cream", Brand = "Olay", Price = 1599, Description = "كريم مضاد للشيخوخة بالكولاجين والببتيد", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355252/Olay_Face_Moisturizer_Regenerist_Micro-Sculpting_Cream_for_Women_Fragrance-Free_-_Anti-Aging_Anti-Wrinkle_Firming_Skin_Care_-_Triple_Collagen_Cream_Peptide_Hyaluronic_Acid_Niacinamide_1.7oz_ttxulg.jpg", Category = "Moisturizer" },
                new Product { Name = "Neutrogena Ultra Sheer Sunscreen SPF30", Brand = "Neutrogena", Price = 1100, Description = "واقي شمس معدني خفيف للوجه والجسم", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355250/Neutrogena_Ultra_Sheer_Dry-Touch_Mineral_Sunscreen_For_Face_Body_SPF_30_Broad-Spectrum_UVA.UVB_m54tup.jpg", Category = "Sunscreen" },
                new Product { Name = "Neutrogena Rapid Tone Repair Retinol", Brand = "Neutrogena", Price = 1350, Description = "كريم ريتينول وفيتامين C لتوحيد البشرة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355250/Neutrogena_Rapid_Tone_Repair_Retinol_Vitamin_C_Face_Moisturizer_Dark_Spot_Corrector_Anti-Aging_Face_Cream_for_Even_Tone_1.7_oz_Trial_Size_Hydro_Boost_Facial_Cleanser_q7pfo4.jpg", Category = "Moisturizer" },
                new Product { Name = "NIVEA Soft Refreshingly Moisturizing Cream", Brand = "NIVEA", Price = 1200, Description = "كريم مرطب خفيف للوجه والجسم", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355251/NIVEA_Soft_Refreshingly_Soft_Moisturizing_Cream_Lightweight_Moisturizer_for_Face_Body_and_igeyux.webp", Category = "Moisturizer" },
                new Product { Name = "NIVEA Soft Light Moisturizer Cream", Brand = "NIVEA", Price = 1100, Description = "كريم مرطب بفيتامين E وزيت الجوجوبا", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355251/NIVEA_Soft_Light_Moisturizer_Cream_with_Vitamin_E_Jojoba_Oil_for_Face_Hands_and_Body_100_e5pcja.webp", Category = "Moisturizer" },
                new Product { Name = "Marini SkinSolutions NeuroSmooth Serum", Brand = "Marini", Price = 1620, Description = "سيروم ببتيد لبشرة ناعمة كالزجاج", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355250/Marini_SkinSolutions_NeuroSmooth_-_Neuromodulating_Peptide-Powered_Face_Serum_for_Glass-Smooth_Poreless-Looking_Skin_-_Cruelty_Free_-_Made_in_the_USA_-_1_fl_oz_pcnhkj.jpg", Category = "Serum" },
                new Product { Name = "MEDITHERAPY Retinal Skin Booster Serum", Brand = "MEDITHERAPY", Price = 1440, Description = "سيروم ريتينال ونياسيناميد لمكافحة الشيخوخة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355250/MEDITHERAPY_Retinal_Skin_Booster_Serum_Retinaldehyde_Niacinamide_Anti-Aging_Serum_twdni8.webp", Category = "Serum" },
                new Product { Name = "Lift Up Light Cream K-Beauty", Brand = "K-Beauty", Price = 960, Description = "كريم شد بالسنتيلا والهيالورونيك", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355249/Lift_Up_Light_Cream_50g_K-Beauty_Firming_Hydrating_Face_Cream_with_Centella_Hyaluronic_kniclx.webp", Category = "Moisturizer" },
                new Product { Name = "La Roche-Posay Toleriane Foaming Cleanser", Brand = "La Roche-Posay", Price = 1300, Description = "غسول رغوي بالنياسيناميد والسيراميد", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355249/La_Roche-Posay_Toleriane_Purifying_Foaming_Facial_Cleanser_Oil_Free_Face_Wash_for_Women_Men_with_Niacinamide_Ceramides_Pore_gmmfkk.jpg", Category = "Cleanser" },
                new Product { Name = "La Roche-Posay Toleriane Hydrating Cleanser", Brand = "La Roche-Posay", Price = 1200, Description = "غسول مرطب للبشرة الجافة بالسيراميد", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355249/La_Roche-Posay_Toleriane_Hydrating_Gentle_Face_Cleanser_Hydrating_Facial_Cleanser_With_Niacinamide_Ceramides_Daily_Face_Wash_For_Dry_Skin_or6an3.jpg", Category = "Cleanser" },
                new Product { Name = "La Roche-Posay Toleriane Double Repair", Brand = "La Roche-Posay", Price = 520, Description = "مرطب بالسيراميد والنياسيناميد لجميع أنواع البشرة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355249/La_Roche-Posay_Toleriane_Double_Repair_Face_Moisturizer_Daily_Moisturizer_Face_Cream_with_Ceramide_Niacinamide_for_All_Skin_kkm1na.jpg", Category = "Moisturizer" },
                new Product { Name = "La Roche-Posay Hyalu B5 Serum", Brand = "La Roche-Posay", Price = 580, Description = "سيروم هيالورونيك أسيد وفيتامين B5", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355248/La_Roche-Posay_Hyalu_B5_Pure_Hyaluronic_Acid_Serum_for_Face_Vitamin_B5_Hyaluronic_Acid_Madecassoside_Hydrating_Serum_hbwctm.jpg", Category = "Serum" },
                new Product { Name = "Jurlique Age-Defying Firming Face Oil", Brand = "Jurlique", Price = 680, Description = "زيت وجه مضاد للشيخوخة لشد البشرة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355247/Jurlique_Purely_Age-Defying_Firming_Face_Oil_Anti-Aging_Serum_1.6_Fl_Oz_Pack_of_1_snumpk.webp", Category = "Serum" },
                new Product { Name = "LANEIGE Lip Glowy Balm", Brand = "LANEIGE", Price = 180, Description = "بالم شفاه مرطب بزبدة الشيا", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355250/LANEIGE_Lip_Glowy_Balm_Sheer_Tinted_Lip_Moisturizer_with_Shea_Butter_for_Hydrating_Shine_Soft_mals7s.webp", Category = "Lip Care" },
                new Product { Name = "L'Oreal Magic Skin BB Cream", Brand = "L'Oreal", Price = 220, Description = "BB كريم مرطب خفيف للاستخدام اليومي", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355249/L_Or%C3%A9al_Paris_Makeup_Magic_Skin_Beautifier_BB_Cream_Tinted_Moisturizer_Light_1_fl_oz_1_Count_h0taro.webp", Category = "Moisturizer" },
                new Product { Name = "La Roche-Posay Anthelios Clear Skin SPF60", Brand = "La Roche-Posay", Price = 860, Description = "واقي شمس SPF60 للبشرة الدهنية", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355247/La_Roche-Posay_Anthelios_Clear_Skin_Sunscreen_Dry_Touch_SPF_60_Oil_Free_Sunscreen_For_Face_Oil_rbbddz.jpg", Category = "Sunscreen" },
                new Product { Name = "La Roche-Posay Anthelios Light Fluid SPF60", Brand = "La Roche-Posay", Price = 980, Description = "واقي شمس سائل خفيف للوجه SPF60", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355248/La_Roche-Posay_Anthelios_Light_Fluid_Facial_Sunscreen_SPF_60_Lightweight_Sunscreen_For_Face_levzdp.jpg", Category = "Sunscreen" },
                new Product { Name = "La Roche-Posay Anthelios UV Sport SPF60", Brand = "La Roche-Posay", Price = 1060, Description = "واقي شمس رياضي مقاوم للماء SPF60", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355248/La_Roche-Posay_Anthelios_UV_Pro-Sport_Sunscreen_for_Face_Body_Water_Sweat_Resistant_Invisible_Broad_Spectrum_Sunscreen_Lightweight_Breathable_Skin_Sun_mzytyb.jpg", Category = "Sunscreen" },
                new Product { Name = "CeraVe Skin Renewing Night Cream", Brand = "CeraVe", Price = 1200, Description = "كريم ليلي بالنياسيناميد والببتيد", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355244/CeraVe_Skin_Renewing_Night_Cream_Niacinamide_Peptide_Complex_and_Hyaluronic_Acid_imprig.webp", Category = "Moisturizer" },
                new Product { Name = "CeraVe Ultra-Light Moisturizing Gel", Brand = "CeraVe", Price = 1300, Description = "جيل مرطب خفيف بالسيراميد والنياسيناميد", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355245/CeraVe_Ultra-Light_Moisturizing_Gel_Hydrating_Gel_Face_Moisturizer_For_Men_For_Women_with_Ceramides_Niacinamide_Hyaluronic_Acid_Fragrance_Free_Oil-Free_Mattifying_Moisturizer_1.75_FL_Oz_ypebeb.jpg", Category = "Moisturizer" },
                new Product { Name = "Dough Cleansing Foam Amino Acid", Brand = "Dough", Price = 240, Description = "غسول رغوي بالأحماض الأمينية والأرز", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355245/Dough_Cleansing_Foam_with_3_Amino_Acid_Complex_Rice_3_Step_Transforming_Amino_Acid_Facial_Cleanser_ubpe0f.jpg", Category = "Cleanser" },
                new Product { Name = "Dr. Denese HydroShield Retinol Serum", Brand = "Dr. Denese", Price = 1200, Description = "سيروم ريتينول لمكافحة التجاعيد", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355246/Dr._Denese_SkinScience_HydroShield_Retinol_Serum_for_Face_4_oz_Dermatologist_Teste_gm5jd9.webp", Category = "Serum" },
                new Product { Name = "Eva Naturals Anti-Aging Serum Set", Brand = "Eva Naturals", Price = 1380, Description = "مجموعة سيروم فيتامين C وريتينول وهيالورونيك", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355246/Eva_Naturals_Anti-Aging_Serum_Set_Vitamin_C_Retinol_Hyaluronic_Acid_Serums_for_Face_Hydrating_Bundle_for_Skin_Fine_Lines_Wrinkles_Collagen_Firming_a59wgn.jpg", Category = "Serum" },
                new Product { Name = "Garnier Light Complete Fairness Serum", Brand = "Garnier", Price = 1050, Description = "سيروم كريم لتفتيح البشرة وتوحيد لونها", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355247/Garnier_Light_Complete_Fairness_Serum_Cream_45g_gzbvwm.webp", Category = "Serum" },
                new Product { Name = "Biossance Squalane Vitamin C Serum", Brand = "Biossance", Price = 960, Description = "سيروم فيتامين C بالسكوالين للإشراقة", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355244/Biossance_Squalane_Vitamin_C_Dark_Spot_Face_Serum_10_Vitamin_C_Serum_White_Shiitak_lrlali.webp", Category = "Serum" },
                new Product { Name = "Burt's Bees Natural Moisturizing Lip Balm", Brand = "Burt's Bees", Price = 95, Description = "بالم شفاه طبيعي بالشمع", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355244/Burt_s_Bees_100_Natural_Moisturizing_Lip_Balm_Original_Beeswax_gvuivz.webp", Category = "Lip Care" },
                new Product { Name = "COSRX Full Fit Propolis Light Cream", Brand = "COSRX", Price = 1500, Description = "كريم خفيف بالبروبوليس للترطيب والتغذية", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355245/COSRX_Full_Fit_Propolis_Light_Cream_2.19_Fl.oz_pqhljg.webp", Category = "Moisturizer" },
                new Product { Name = "CeraVe Intensive Moisturizing Lotion", Brand = "CeraVe", Price = 980, Description = "لوشن مكثف بالهيدرو يوريا وزبدة الشيا", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355244/CeraVe_Intensive_Moisturizing_Lotion_Hydro-Urea_Shea_Butter_Body_Lotion_For_Dry_Skin_Relieves_Signs_Of_Extra_Dry_Skin_Non_Greasy_Hydrating_Cream_For_Rough_Tight_Red_Itchy_Skin_8oz_za6bmi.jpg", Category = "Body Care" },
                new Product { Name = "CeraVe Moisturizing Cream", Brand = "CeraVe", Price = 450, Description = "مرطب للوجه والجسم بالهيالورونيك والسيراميد", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355245/CeraVe_Moisturizing_Cream_Body_and_Face_Moisturizer_for_Dry_Skin_Body_Cream_with_Hyaluronic_Acid_and_Ceramides_Daily_Moisturizer_Oil-Free_Fragrance_Free_Non-Comedogenic_b0k9is.jpg", Category = "Moisturizer" },
                new Product { Name = "CeraVe Resurfacing Retinol Serum", Brand = "CeraVe", Price = 1060, Description = "سيروم الريتينول بمستخلص عرق السوس", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355245/CeraVe_Resurfacing_Retinol_Serum_For_Post_Acne_Marks_Formulated_With_Licorice_Root_Extract_bpzot8.webp", Category = "Serum" },
                new Product { Name = "2PC Relief Sun Organic Sunscreen SPF50", Brand = "Relief Sun", Price = 1280, Description = "واقي شمس كوري عضوي بالأرز والبروبيوتيك", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355243/2PC_Relief_Sun_Organic_Sunscreen_SPF50_Korean_Skin_Care_Solution_for_All_Skin_Types_PA_Rice_and_Probiotics_A_Sun_Protection_Factor_SPF_50_qr9jcb.jpg", Category = "Sunscreen" },
                new Product { Name = "Aquaphor Lip Repair Ointment", Brand = "Aquaphor", Price = 1050, Description = "مرهم إصلاح الشفاه لعلاج الجفاف", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355244/Aquaphor_Lip_Repair_Ointment_Moisturizing_Lip_Balm_Pack_Relieves_Dryness_0.35_Tube_Pack_j5ihji.webp", Category = "Lip Care" },
                new Product { Name = "BIOAQUA V7 Toning Light Cream", Brand = "BIOAQUA", Price = 1650, Description = "كريم متعدد الفيتامينات والشوفان للترطيب", ImageUrl = "https://res.cloudinary.com/ddmkfyhkf/image/upload/v1780355244/BIOAQUA_V7_Toning_Light_Cream_For_Lazy_Makeup_Multivitamin_Complex_Oat_Hyaluronic_Acid_nt1jip.webp", Category = "Moisturizer" },
            };

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Products seeded successfully!", count = products.Count });
        }
    }
}
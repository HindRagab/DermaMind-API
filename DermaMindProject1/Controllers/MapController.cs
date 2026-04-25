using Microsoft.AspNetCore.Mvc;

namespace DermaApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        // ✅ فتح Google Maps للصيدليات القريبة
        [HttpGet("pharmacies")]
        public IActionResult GetNearbyPharmacies(
            [FromQuery] double latitude,
            [FromQuery] double longitude)
        {
            var mapsUrl = $"https://www.google.com/maps/search/pharmacy/@{latitude},{longitude},15z";

            return Ok(new
            {
                mapsUrl,
                message = "Open this URL to see nearby pharmacies"
            });
        }

        // ✅ فتح Google Maps للعيادات الجلدية القريبة
        [HttpGet("clinics")]
        public IActionResult GetNearbyClinics(
            [FromQuery] double latitude,
            [FromQuery] double longitude)
        {
            var mapsUrl = $"https://www.google.com/maps/search/dermatology+clinic/@{latitude},{longitude},15z";

            return Ok(new
            {
                mapsUrl,
                message = "Open this URL to see nearby clinics"
            });
        }
    }
}
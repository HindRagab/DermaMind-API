using System.ComponentModel.DataAnnotations.Schema;

namespace DermaApp.API.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Brand { get; set; }
        public string Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
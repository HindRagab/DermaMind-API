using Microsoft.EntityFrameworkCore.Migrations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DermaApp.API.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymobOrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
      
       public int OrderId { get; set; }
        [Column(TypeName = "decimal(18,2)")]

        public decimal TotalAmount { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
    }
}
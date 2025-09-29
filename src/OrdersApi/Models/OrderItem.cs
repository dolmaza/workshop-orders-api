using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Models;

public class OrderItem
{
    public Guid Id { get; set; }
    
    public Guid OrderId { get; set; }
    
    public Order Order { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ProductSku { get; set; } = string.Empty;
    
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }
    
    public decimal TotalPrice => Quantity * UnitPrice;
}
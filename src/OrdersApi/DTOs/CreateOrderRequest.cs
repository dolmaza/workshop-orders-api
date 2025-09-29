using System.ComponentModel.DataAnnotations;

namespace OrdersApi.DTOs;

public class CreateOrderRequest
{
    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;
    
    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
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
}
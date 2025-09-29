using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Models;

public class Order
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;
    
    public DateTime OrderDate { get; set; }
    
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public decimal TotalAmount { get; set; }
    
    public List<OrderItem> Items { get; set; } = new();
    
    public DateTime? CancelledAt { get; set; }
    
    public string? CancellationReason { get; set; }
}
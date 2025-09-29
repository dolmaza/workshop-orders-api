using System.ComponentModel.DataAnnotations;

namespace OrdersApi.DTOs;

public class CancelOrderRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
using OrdersApi.DTOs;
using OrdersApi.Models;

namespace OrdersApi.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderResponse>> GetAllOrdersAsync();
    Task<OrderResponse?> GetOrderByIdAsync(Guid id);
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResponse?> CancelOrderAsync(Guid id, CancelOrderRequest request);
}
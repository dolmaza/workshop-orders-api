using OrdersApi.DTOs;
using OrdersApi.Models;
using OrdersApi.Repositories;

namespace OrdersApi.Services;

public class OrderService(IOrderRepository orderRepository) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;

    public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(MapToOrderResponse);
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order == null ? null : MapToOrderResponse(order);
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = request.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductName = item.ProductName,
                ProductSku = item.ProductSku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        // Calculate total amount
        order.TotalAmount = order.Items.Sum(item => item.TotalPrice);

        var createdOrder = await _orderRepository.CreateAsync(order);
        return MapToOrderResponse(createdOrder);
    }

    public async Task<OrderResponse?> CancelOrderAsync(Guid id, CancelOrderRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return null;

        // Business rule: Only pending or confirmed orders can be cancelled
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException($"Cannot cancel order with status {order.Status}");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancellationReason = request.Reason;

        var updatedOrder = await _orderRepository.UpdateAsync(order);
        return MapToOrderResponse(updatedOrder);
    }

    private static OrderResponse MapToOrderResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CancelledAt = order.CancelledAt,
            CancellationReason = order.CancellationReason,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                Id = item.Id,
                ProductName = item.ProductName,
                ProductSku = item.ProductSku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
    }
}
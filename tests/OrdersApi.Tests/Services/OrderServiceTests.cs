using FluentAssertions;
using Moq;
using OrdersApi.DTOs;
using OrdersApi.Models;
using OrdersApi.Repositories;
using OrdersApi.Services;
using Xunit;

namespace OrdersApi.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _orderService = new OrderService(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllOrdersAsync_ShouldReturnMappedOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder(),
            CreateTestOrder()
        };
        _orderRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetAllOrdersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllBeOfType<OrderResponse>();
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithValidId_ShouldReturnMappedOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder(orderId);
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _orderService.GetOrderByIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.CustomerName.Should().Be(order.CustomerName);
        result.CustomerEmail.Should().Be(order.CustomerEmail);
        result.Items.Should().HaveCount(order.Items.Count);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.GetOrderByIdAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidRequest_ShouldCreateAndReturnOrder()
    {
        // Arrange
        var request = CreateTestOrderRequest();
        var expectedOrder = CreateTestOrder();
        _orderRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CustomerName.Should().Be(request.CustomerName);
        result.CustomerEmail.Should().Be(request.CustomerEmail);
        result.Status.Should().Be(OrderStatus.Pending);
        result.Items.Should().HaveCount(request.Items.Count);
        result.TotalAmount.Should().Be(request.Items.Sum(i => i.Quantity * i.UnitPrice));

        _orderRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCalculateTotalAmountCorrectly()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerName = "Test Customer",
            CustomerEmail = "test@example.com",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductName = "Product 1", ProductSku = "SKU1", Quantity = 2, UnitPrice = 10.50m },
                new() { ProductName = "Product 2", ProductSku = "SKU2", Quantity = 1, UnitPrice = 15.25m }
            }
        };

        _orderRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        result.TotalAmount.Should().Be(36.25m); // (2 * 10.50) + (1 * 15.25)
    }

    [Fact]
    public async Task CancelOrderAsync_WithValidOrderAndPendingStatus_ShouldCancelOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder(orderId);
        order.Status = OrderStatus.Pending;
        
        var cancelRequest = new CancelOrderRequest { Reason = "Customer request" };
        
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
        _orderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _orderService.CancelOrderAsync(orderId, cancelRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderStatus.Cancelled);
        result.CancellationReason.Should().Be(cancelRequest.Reason);
        result.CancelledAt.Should().NotBeNull();
        result.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Order>(o => 
            o.Status == OrderStatus.Cancelled && 
            o.CancellationReason == cancelRequest.Reason)), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_WithValidOrderAndConfirmedStatus_ShouldCancelOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder(orderId);
        order.Status = OrderStatus.Confirmed;
        
        var cancelRequest = new CancelOrderRequest { Reason = "Inventory issue" };
        
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
        _orderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _orderService.CancelOrderAsync(orderId, cancelRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelOrderAsync_WithInvalidOrderId_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cancelRequest = new CancelOrderRequest { Reason = "Customer request" };
        
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.CancelOrderAsync(orderId, cancelRequest);

        // Assert
        result.Should().BeNull();
        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task CancelOrderAsync_WithInvalidStatus_ShouldThrowInvalidOperationException(OrderStatus status)
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder(orderId);
        order.Status = status;
        
        var cancelRequest = new CancelOrderRequest { Reason = "Customer request" };
        
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _orderService.CancelOrderAsync(orderId, cancelRequest));
        
        exception.Message.Should().Contain($"Cannot cancel order with status {status}");
        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    private static Order CreateTestOrder(Guid? id = null)
    {
        return new Order
        {
            Id = id ?? Guid.NewGuid(),
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Test Product",
                    ProductSku = "TEST-SKU",
                    Quantity = 2,
                    UnitPrice = 50.00m
                }
            }
        };
    }

    private static CreateOrderRequest CreateTestOrderRequest()
    {
        return new CreateOrderRequest
        {
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    ProductSku = "TEST-SKU",
                    Quantity = 2,
                    UnitPrice = 50.00m
                }
            }
        };
    }
}
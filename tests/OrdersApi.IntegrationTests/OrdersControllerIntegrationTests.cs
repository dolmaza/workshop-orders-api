using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrdersApi.Data;
using OrdersApi.DTOs;
using OrdersApi.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OrdersApi.IntegrationTests;

public class OrdersControllerIntegrationTests : IClassFixture<OrdersApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly OrdersApiWebApplicationFactory _factory;

    public OrdersControllerIntegrationTests(OrdersApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Ensure a clean database state for each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        context.Orders.RemoveRange(context.Orders);
        context.OrderItems.RemoveRange(context.OrderItems);
        context.SaveChanges();
    }

    [Fact]
    public async Task GetAllOrders_ShouldReturnEmptyList_WhenNoOrdersExist()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderResponse>>();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllOrders_ShouldReturnOrders_WhenOrdersExist()
    {
        // Arrange
        await SeedTestOrder();

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderResponse>>();
        orders.Should().HaveCount(1);
        orders![0].CustomerName.Should().Be("Test Customer");
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var order = await SeedTestOrder();

        // Act
        var response = await _client.GetAsync($"/api/orders/{order.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedOrder = await response.Content.ReadFromJsonAsync<OrderResponse>();
        returnedOrder.Should().NotBeNull();
        returnedOrder!.Id.Should().Be(order.Id);
        returnedOrder.CustomerName.Should().Be(order.CustomerName);
        returnedOrder.Items.Should().HaveCount(order.Items.Count);
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/orders/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_ShouldCreateOrder_WithValidRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    ProductSku = "TEST-001",
                    Quantity = 2,
                    UnitPrice = 25.50m
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdOrder = await response.Content.ReadFromJsonAsync<OrderResponse>();
        
        createdOrder.Should().NotBeNull();
        createdOrder!.Id.Should().NotBeEmpty();
        createdOrder.CustomerName.Should().Be(request.CustomerName);
        createdOrder.CustomerEmail.Should().Be(request.CustomerEmail);
        createdOrder.Status.Should().Be(OrderStatus.Pending);
        createdOrder.TotalAmount.Should().Be(51.00m); // 2 * 25.50
        createdOrder.Items.Should().HaveCount(1);
        createdOrder.Items[0].ProductName.Should().Be("Test Product");

        // Verify the order was actually created in the database
        var getResponse = await _client.GetAsync($"/api/orders/{createdOrder.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequest_WithInvalidRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerName = "", // Invalid: empty name
            CustomerEmail = "invalid-email", // Invalid: bad email format
            Items = new List<CreateOrderItemRequest>() // Invalid: no items
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_ShouldCalculateTotalCorrectly_WithMultipleItems()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerName = "Jane Smith",
            CustomerEmail = "jane.smith@example.com",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductName = "Product A", ProductSku = "A001", Quantity = 3, UnitPrice = 10.00m },
                new() { ProductName = "Product B", ProductSku = "B001", Quantity = 2, UnitPrice = 15.50m }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdOrder = await response.Content.ReadFromJsonAsync<OrderResponse>();
        
        createdOrder!.TotalAmount.Should().Be(61.00m); // (3 * 10.00) + (2 * 15.50)
        createdOrder.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CancelOrder_ShouldCancelOrder_WhenOrderIsPending()
    {
        // Arrange
        var order = await SeedTestOrder();
        var cancelRequest = new CancelOrderRequest { Reason = "Customer changed mind" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/orders/{order.Id}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelledOrder = await response.Content.ReadFromJsonAsync<OrderResponse>();
        
        cancelledOrder.Should().NotBeNull();
        cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);
        cancelledOrder.CancellationReason.Should().Be(cancelRequest.Reason);
        cancelledOrder.CancelledAt.Should().NotBeNull();
        cancelledOrder.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CancelOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var cancelRequest = new CancelOrderRequest { Reason = "Test reason" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/orders/{nonExistentId}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelOrder_ShouldReturnBadRequest_WhenOrderIsShipped()
    {
        // Arrange
        var order = await SeedTestOrder();
        await UpdateOrderStatus(order.Id, OrderStatus.Shipped);
        
        var cancelRequest = new CancelOrderRequest { Reason = "Too late" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/orders/{order.Id}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cannot cancel order with status Shipped");
    }

    [Fact]
    public async Task CancelOrder_ShouldReturnBadRequest_WithInvalidRequest()
    {
        // Arrange
        var order = await SeedTestOrder();
        var cancelRequest = new CancelOrderRequest { Reason = "" }; // Invalid: empty reason

        // Act
        var response = await _client.PostAsJsonAsync($"/api/orders/{order.Id}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApiEndpoints_ShouldReturnProperContentType()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnLocationHeader()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerName = "Test Customer",
            CustomerEmail = "test@example.com",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductName = "Product", ProductSku = "SKU", Quantity = 1, UnitPrice = 10.00m }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/orders/");
    }

    private async Task<Order> SeedTestOrder()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerEmail = "test@example.com",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Test Product",
                    ProductSku = "TEST-001",
                    Quantity = 2,
                    UnitPrice = 50.00m
                }
            }
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    private async Task UpdateOrderStatus(Guid orderId, OrderStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        
        var order = await context.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = status;
            await context.SaveChangesAsync();
        }
    }
}
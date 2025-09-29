using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;
using OrdersApi.Repositories;
using Xunit;

namespace OrdersApi.Tests.Repositories;

public class OrderRepositoryTests : IDisposable
{
    private readonly OrdersDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrdersDbContext(options);
        _repository = new OrderRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllOrdersWithItems()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(order => order.Items.Should().NotBeEmpty());
        result.Should().BeInDescendingOrder(order => order.OrderDate);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnOrderWithItems()
    {
        // Arrange
        var order = await SeedSingleOrder();

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.CustomerName.Should().Be(order.CustomerName);
        result.Items.Should().HaveCount(order.Items.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddOrderToDatabase()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        var result = await _repository.CreateAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();

        var savedOrder = await _context.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == result.Id);
        savedOrder.Should().NotBeNull();
        savedOrder!.Items.Should().HaveCount(order.Items.Count);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingOrder()
    {
        // Arrange
        var order = await SeedSingleOrder();
        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = "Customer request";

        // Act
        var result = await _repository.UpdateAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Cancelled);
        result.CancellationReason.Should().Be("Customer request");

        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);
        updatedOrder.CancellationReason.Should().Be("Customer request");
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemoveOrderAndReturnTrue()
    {
        // Arrange
        var order = await SeedSingleOrder();

        // Act
        var result = await _repository.DeleteAsync(order.Id);

        // Assert
        result.Should().BeTrue();

        var deletedOrder = await _context.Orders.FindAsync(order.Id);
        deletedOrder.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        var order = await SeedSingleOrder();

        // Act
        var result = await _repository.ExistsAsync(order.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    private async Task<Order> SeedSingleOrder()
    {
        var order = CreateTestOrder();
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    private async Task SeedTestData()
    {
        var orders = new List<Order>
        {
            CreateTestOrder("Customer 1", "customer1@example.com", DateTime.UtcNow.AddHours(-2)),
            CreateTestOrder("Customer 2", "customer2@example.com", DateTime.UtcNow.AddHours(-1))
        };

        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();
    }

    private static Order CreateTestOrder(string customerName = "Test Customer", 
        string customerEmail = "test@example.com", DateTime? orderDate = null)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            OrderDate = orderDate ?? DateTime.UtcNow,
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

    public void Dispose()
    {
        _context.Dispose();
    }
}
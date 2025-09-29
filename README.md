# Crafting Reliable Software: A Hands-On Workshop on Testing in .NET

## Workshop Overview

Welcome to this hands-on workshop where you'll learn to write reliable software using comprehensive testing strategies in .NET 8. This workshop demonstrates both **unit testing** and **integration testing** patterns using a real-world Orders API.

## 📋 Current Project Structure

```
orders-api/
├── src/
│   └── OrdersApi/                    # Main API project
│       ├── Controllers/              # API controllers
│       ├── Models/                   # Domain models (Order, OrderItem, OrderStatus)
│       ├── Services/                 # Business logic layer
│       ├── Repositories/             # Data access layer
│       ├── DTOs/                     # Data transfer objects
│       └── Data/                     # Entity Framework DbContext
├── tests/
│   ├── OrdersApi.Tests/              # Unit tests
│   │   ├── Services/                 # Service layer unit tests
│   │   └── Repositories/             # Repository layer unit tests
│   └── OrdersApi.IntegrationTests/   # Integration tests
│       └── OrdersControllerIntegrationTests.cs
```

## 🎯 Learning Objectives

By the end of this workshop, you will understand:

1. **Unit Testing Fundamentals**
   - Writing isolated tests using mocking (Moq)
   - Testing business logic in services
   - Testing data access logic in repositories
   - Using FluentAssertions for readable assertions

2. **Integration Testing Patterns**
   - Testing complete API workflows
   - Using WebApplicationFactory for in-memory testing
   - Database testing with Entity Framework InMemory provider
   - HTTP client testing strategies

3. **Test Organization and Best Practices**
   - Arrange-Act-Assert pattern
   - Test naming conventions
   - Test data setup and teardown
   - Parameterized tests with Theory/InlineData

## 🔍 Existing Implementation

### Domain Models
- **Order**: Core entity with customer info, status tracking, and cancellation support
- **OrderItem**: Individual items within an order with pricing
- **OrderStatus**: Enum representing order lifecycle (Pending → Confirmed → Shipped → Delivered | Cancelled)

### Current Features
- ✅ Create new orders
- ✅ Get all orders
- ✅ Get order by ID
- ✅ Cancel orders (with business rules)
- ✅ Full CRUD operations

### Existing Tests
- **Unit Tests**: Comprehensive coverage of OrderService and OrderRepository
- **Integration Tests**: Full API workflow testing including HTTP requests/responses

## 🚀 **1-Hour Workshop Exercise**

### Your Mission: Implement Order Confirmation Feature

You need to implement a new **order confirmation** feature that allows orders to be moved from "Pending" to "Confirmed" status, along with comprehensive tests.

### 📋 **Task Requirements**

#### **Part 1: Extend the Domain (15 minutes)**

1. **Add new properties to Order model**:
   - `DateTime? ConfirmedAt` - When the order was confirmed
   - `string? ConfirmedBy` - Who confirmed the order (employee/system name)

2. **Business Rules for Confirmation**:
   - Only orders with status `Pending` can be confirmed
   - Orders with status `Confirmed`, `Shipped`, `Delivered`, or `Cancelled` cannot be confirmed
   - Must provide who confirmed the order
   - Confirmation timestamp should be set automatically

#### **Part 2: Create DTOs (5 minutes)**

3. **Create `ConfirmOrderRequest` DTO**:
   ```csharp
   public class ConfirmOrderRequest
   {
       public string ConfirmedBy { get; set; } = string.Empty;
   }
   ```

4. **Update `OrderResponse` DTO** to include new confirmation fields

#### **Part 3: Implement Business Logic (15 minutes)**

5. **Add to `IOrderService` interface**:
   ```csharp
   Task<OrderResponse?> ConfirmOrderAsync(Guid orderId, ConfirmOrderRequest request);
   ```

6. **Implement in `OrderService`**:
   - Validate order exists
   - Check business rules (only Pending orders can be confirmed)
   - Update order status to Confirmed
   - Set confirmation timestamp and confirmer
   - Return updated order

#### **Part 4: Add API Endpoint (5 minutes)**

7. **Add to `OrdersController`**:
   ```csharp
   [HttpPost("{id}/confirm")]
   public async Task<ActionResult<OrderResponse>> ConfirmOrder(Guid id, ConfirmOrderRequest request)
   ```

#### **Part 5: Write Unit Tests (15 minutes)**

8. **Add unit tests to `OrderServiceTests`**:
   - ✅ `ConfirmOrderAsync_WithValidOrderAndPendingStatus_ShouldConfirmOrder`
   - ✅ `ConfirmOrderAsync_WithInvalidOrderId_ShouldReturnNull`
   - ✅ `ConfirmOrderAsync_WithConfirmedStatus_ShouldThrowInvalidOperationException`
   - ✅ `ConfirmOrderAsync_WithShippedStatus_ShouldThrowInvalidOperationException`
   - ✅ `ConfirmOrderAsync_WithCancelledStatus_ShouldThrowInvalidOperationException`
   - ✅ `ConfirmOrderAsync_ShouldSetConfirmationTimestamp`

#### **Part 6: Write Integration Tests (5 minutes)**

9. **Add integration tests to `OrdersControllerIntegrationTests`**:
   - ✅ `ConfirmOrder_ShouldConfirmOrder_WhenOrderIsPending`
   - ✅ `ConfirmOrder_ShouldReturnNotFound_WhenOrderDoesNotExist`
   - ✅ `ConfirmOrder_ShouldReturnBadRequest_WhenOrderIsNotPending`
   - ✅ `ConfirmOrder_ShouldReturnBadRequest_WithInvalidRequest`

## 🧪 **Testing Patterns to Implement**

### **Unit Testing Patterns**
```csharp
[Fact]
public async Task ConfirmOrderAsync_WithValidOrderAndPendingStatus_ShouldConfirmOrder()
{
    // Arrange
    var orderId = Guid.NewGuid();
    var order = CreateTestOrder(orderId);
    order.Status = OrderStatus.Pending;
    
    var confirmRequest = new ConfirmOrderRequest { ConfirmedBy = "John Manager" };
    
    _orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
    _orderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
        .ReturnsAsync((Order o) => o);

    // Act
    var result = await _orderService.ConfirmOrderAsync(orderId, confirmRequest);

    // Assert
    result.Should().NotBeNull();
    result!.Status.Should().Be(OrderStatus.Confirmed);
    result.ConfirmedBy.Should().Be(confirmRequest.ConfirmedBy);
    result.ConfirmedAt.Should().NotBeNull();
    result.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

    _orderRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Order>(o => 
        o.Status == OrderStatus.Confirmed && 
        o.ConfirmedBy == confirmRequest.ConfirmedBy)), Times.Once);
}
```

### **Integration Testing Patterns**
```csharp
[Fact]
public async Task ConfirmOrder_ShouldConfirmOrder_WhenOrderIsPending()
{
    // Arrange
    var order = await SeedTestOrder();
    var confirmRequest = new ConfirmOrderRequest { ConfirmedBy = "Store Manager" };

    // Act
    var response = await _client.PostAsJsonAsync($"/api/orders/{order.Id}/confirm", confirmRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var confirmedOrder = await response.Content.ReadFromJsonAsync<OrderResponse>();
    
    confirmedOrder.Should().NotBeNull();
    confirmedOrder!.Status.Should().Be(OrderStatus.Confirmed);
    confirmedOrder.ConfirmedBy.Should().Be(confirmRequest.ConfirmedBy);
    confirmedOrder.ConfirmedAt.Should().NotBeNull();
}
```

## 🛠️ **Getting Started**

1. **Build and run tests**:
   ```bash
   dotnet build
   dotnet test
   ```

2. **Start with failing tests** - Write tests first, then implement!

3. **Follow TDD approach**:
   - Write failing test
   - Implement minimal code to pass
   - Refactor and improve

## 🎯 **Success Criteria**

- [ ] All existing tests continue to pass
- [ ] New confirmation feature works end-to-end
- [ ] Unit tests cover all business logic scenarios
- [ ] Integration tests verify API contracts
- [ ] Business rules are properly enforced
- [ ] Error handling is comprehensive

## 📚 **Key Testing Concepts Demonstrated**

1. **Mocking with Moq**: Isolating units under test
2. **FluentAssertions**: Writing expressive, readable assertions
3. **WebApplicationFactory**: Testing ASP.NET Core applications
4. **InMemory Database**: Fast, isolated integration tests
5. **Test Data Builders**: Creating consistent test data
6. **Parameterized Tests**: Testing multiple scenarios efficiently

## 🔧 **Tools and Libraries Used**

- **xUnit**: Testing framework
- **Moq**: Mocking framework for unit tests
- **FluentAssertions**: Assertion library for readable tests
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing support
- **Entity Framework InMemory**: In-memory database for testing

## 💡 **Tips for Success**

1. **Start with the failing test** - Know what you're building
2. **Keep tests simple** - One concept per test
3. **Use descriptive test names** - Should read like documentation
4. **Arrange-Act-Assert** - Clear test structure
5. **Test edge cases** - Don't just test the happy path

---

**Happy Testing! 🧪✨**

Remember: Good tests are your safety net for reliable software. They give you confidence to refactor, add features, and deploy with certainty!
using Microsoft.AspNetCore.Mvc;
using OrdersApi.DTOs;
using OrdersApi.Services;
using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController(IOrderService orderService, ILogger<OrdersController> logger) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;
    private readonly ILogger<OrdersController> _logger = logger;

    /// <summary>
    /// Get all orders
    /// </summary>
    /// <returns>List of all orders</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAllOrders()
    {
        _logger.LogInformation("Getting all orders");
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id)
    {
        _logger.LogInformation("Getting order with ID: {OrderId}", id);
        var order = await _orderService.GetOrderByIdAsync(id);
        
        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found", id);
            return NotFound($"Order with ID {id} not found");
        }

        return Ok(order);
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="request">Order creation request</param>
    /// <returns>Created order details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for order creation: {@ValidationErrors}", ModelState);
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Creating new order for customer: {CustomerEmail}", request.CustomerEmail);
            var order = await _orderService.CreateOrderAsync(request);
            
            _logger.LogInformation("Order created successfully with ID: {OrderId}", order.Id);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for customer: {CustomerEmail}", request.CustomerEmail);
            return BadRequest("An error occurred while creating the order");
        }
    }

    /// <summary>
    /// Cancel an existing order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="request">Cancellation request</param>
    /// <returns>Updated order details</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for order cancellation: {@ValidationErrors}", ModelState);
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Cancelling order with ID: {OrderId}", id);
            var order = await _orderService.CancelOrderAsync(id, request);
            
            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found for cancellation", id);
                return NotFound($"Order with ID {id} not found");
            }

            _logger.LogInformation("Order with ID {OrderId} cancelled successfully", id);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while cancelling order {OrderId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order with ID: {OrderId}", id);
            return BadRequest("An error occurred while cancelling the order");
        }
    }
}
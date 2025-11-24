using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.DTOs;
using POS.Api.Services;
using System.Security.Claims;

namespace POS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            var order = await _orderService.CreateOrderAsync(createOrderDto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderDto orderDto)
        {
            var result = await _orderService.UpdateOrderAsync(id, orderDto);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Salesman")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                var result = await _orderService.UpdateOrderStatusAsync(id, updateDto, userId);
                if (!result)
                    return NotFound();

                return Ok(new { message = "Order status updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("bulk-update-status")]
        [Authorize(Roles = "Admin,Salesman")]
        public async Task<IActionResult> BulkUpdateOrderStatus([FromBody] BulkUpdateOrderStatusDto bulkUpdateDto)
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                var updatedCount = await _orderService.BulkUpdateOrderStatusAsync(bulkUpdateDto, userId);
                return Ok(new { message = $"{updatedCount} order(s) updated successfully", updatedCount });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("search-customers")]
        public async Task<ActionResult<IEnumerable<CustomerSearchDto>>> SearchCustomers([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest(new { message = "Search term is required" });

            var customers = await _orderService.SearchCustomersAsync(searchTerm);
            return Ok(customers);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var result = await _orderService.DeleteOrderAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}



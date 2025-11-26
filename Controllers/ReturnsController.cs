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
    public class ReturnsController : ControllerBase
    {
        private readonly IReturnService _returnService;
        private readonly ILogger<ReturnsController> _logger;

        public ReturnsController(IReturnService returnService, ILogger<ReturnsController> logger)
        {
            _returnService = returnService;
            _logger = logger;
        }

        /// <summary>
        /// Get all returns
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReturnDto>>> GetAllReturns()
        {
            try
            {
                var returns = await _returnService.GetAllReturnsAsync();
                return Ok(returns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving returns");
                return StatusCode(500, new { error = "An error occurred while retrieving returns" });
            }
        }

        /// <summary>
        /// Get return by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ReturnDto>> GetReturnById(int id)
        {
            try
            {
                var returnDto = await _returnService.GetReturnByIdAsync(id);
                if (returnDto == null)
                {
                    return NotFound(new { error = $"Return with ID {id} not found" });
                }

                return Ok(returnDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving return {ReturnId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving return" });
            }
        }

        /// <summary>
        /// Create whole bill return
        /// </summary>
        [HttpPost("whole")]
        public async Task<ActionResult<ReturnDto>> CreateWholeReturn([FromBody] WholeReturnRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Received whole return request: InvoiceId={InvoiceId}, OrderId={OrderId}, Amount={Amount}", 
                    request.InvoiceId, request.OrderId, request.TotalReturnAmount);

                var returnDto = await _returnService.CreateWholeReturnAsync(request);
                
                _logger.LogInformation("Whole return created successfully with ID {ReturnId}", returnDto.ReturnId);
                
                return CreatedAtAction(nameof(GetReturnById), new { id = returnDto.ReturnId }, returnDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating whole return for invoice {InvoiceId}", request.InvoiceId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating whole return for invoice {InvoiceId}", request.InvoiceId);
                return StatusCode(500, new { error = "An error occurred while creating return", details = ex.Message });
            }
        }

        /// <summary>
        /// Create partial return
        /// </summary>
        [HttpPost("partial")]
        public async Task<ActionResult<ReturnDto>> CreatePartialReturn([FromBody] PartialReturnRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Received partial return request: InvoiceId={InvoiceId}, OrderId={OrderId}, Items={ItemCount}, Amount={Amount}", 
                    request.InvoiceId, request.OrderId, request.Items.Count, request.TotalReturnAmount);

                var returnDto = await _returnService.CreatePartialReturnAsync(request);
                
                _logger.LogInformation("Partial return created successfully with ID {ReturnId}", returnDto.ReturnId);
                
                return CreatedAtAction(nameof(GetReturnById), new { id = returnDto.ReturnId }, returnDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating partial return for invoice {InvoiceId}", request.InvoiceId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating partial return for invoice {InvoiceId}", request.InvoiceId);
                return StatusCode(500, new { error = "An error occurred while creating return", details = ex.Message });
            }
        }

        /// <summary>
        /// Get return summary statistics
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<ReturnSummaryDto>> GetReturnSummary()
        {
            try
            {
                var summary = await _returnService.GetReturnSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving return summary");
                return StatusCode(500, new { error = "An error occurred while retrieving summary" });
            }
        }

        /// <summary>
        /// Update return status (Admin only)
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateReturnStatus(int id, [FromBody] UpdateReturnStatusRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { error = "Invalid user ID" });
                }

                var result = await _returnService.UpdateReturnStatusAsync(id, request, userId);
                if (!result)
                {
                    return NotFound(new { error = $"Return with ID {id} not found" });
                }

                return Ok(new { message = "Return status updated successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating return {ReturnId} status", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating return {ReturnId} status", id);
                return StatusCode(500, new { error = "An error occurred while updating return status" });
            }
        }
    }
}

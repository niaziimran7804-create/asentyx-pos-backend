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
    public class AccountingController : ControllerBase
    {
        private readonly IAccountingService _accountingService;

        public AccountingController(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        /// <summary>
        /// Get all accounting entries with optional filters and pagination
        /// </summary>
        [HttpGet("entries")]
        public async Task<ActionResult<AccountingEntriesResponseDto>> GetEntries(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? entryType = null,
            [FromQuery] string? paymentMethod = null,
            [FromQuery] string? category = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            try
            {
                var result = await _accountingService.GetAccountingEntriesAsync(
                    startDate, endDate, entryType, paymentMethod, category, page, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Failed to retrieve entries",
                    message = ex.Message,
                    statusCode = 400,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Create a new accounting entry
        /// </summary>
        [HttpPost("entries")]
        public async Task<ActionResult<object>> CreateEntry([FromBody] CreateAccountingEntryDto dto)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var entry = await _accountingService.CreateAccountingEntryAsync(dto, username);

                return CreatedAtAction(
                    nameof(GetEntries),
                    new { },
                    new
                    {
                        message = "Accounting entry created successfully",
                        entryId = entry.EntryId,
                        entry
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    error = "Validation failed",
                    message = ex.Message,
                    statusCode = 400,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message,
                    statusCode = 500,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Delete an accounting entry (Admin only)
        /// </summary>
        [HttpDelete("entries/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> DeleteEntry(int id)
        {
            try
            {
                var result = await _accountingService.DeleteAccountingEntryAsync(id);
                if (!result)
                {
                    return NotFound(new
                    {
                        error = "Not found",
                        message = "Accounting entry not found",
                        statusCode = 404,
                        timestamp = DateTime.UtcNow
                    });
                }

                return Ok(new
                {
                    message = "Accounting entry deleted successfully",
                    entryId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message,
                    statusCode = 500,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get financial summary
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<FinancialSummaryDto>> GetSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _accountingService.GetFinancialSummaryAsync(startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message,
                    statusCode = 500,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get daily sales data
        /// </summary>
        [HttpGet("daily-sales")]
        public async Task<ActionResult<List<DailySalesDto>>> GetDailySales([FromQuery] int days = 7)
        {
            try
            {
                var result = await _accountingService.GetDailySalesAsync(days);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message,
                    statusCode = 500,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get sales graph data
        /// </summary>
        [HttpGet("sales-graph")]
        public async Task<ActionResult<SalesGraphDto>> GetSalesGraph(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var graphData = await _accountingService.GetSalesGraphAsync(startDate, endDate);
                return Ok(graphData);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching sales graph data" });
            }
        }
    }
}

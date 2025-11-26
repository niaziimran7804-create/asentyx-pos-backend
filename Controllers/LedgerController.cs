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
    public class LedgerController : ControllerBase
    {
        private readonly ILedgerService _ledgerService;

        public LedgerController(ILedgerService ledgerService)
        {
            _ledgerService = ledgerService;
        }

        /// <summary>
        /// Get ledger for a specific customer
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<List<CustomerLedgerDto>>> GetCustomerLedger(
            int customerId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var ledger = await _ledgerService.GetCustomerLedgerAsync(customerId, startDate, endDate);
                return Ok(ledger);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve ledger", message = ex.Message });
            }
        }

        /// <summary>
        /// Get customer account statement
        /// </summary>
        [HttpGet("customer/{customerId}/statement")]
        public async Task<ActionResult<CustomerStatementDto>> GetCustomerStatement(
            int customerId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var statement = await _ledgerService.GetCustomerStatementAsync(customerId, startDate, endDate);
                return Ok(statement);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate statement", message = ex.Message });
            }
        }

        /// <summary>
        /// Get customer current balance
        /// </summary>
        [HttpGet("customer/{customerId}/balance")]
        public async Task<ActionResult<object>> GetCustomerBalance(int customerId)
        {
            try
            {
                var balance = await _ledgerService.GetCustomerBalanceAsync(customerId);
                return Ok(new
                {
                    customerId,
                    currentBalance = balance,
                    asOfDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve balance", message = ex.Message });
            }
        }

        /// <summary>
        /// Get customer ledger summary
        /// </summary>
        [HttpGet("customer/{customerId}/summary")]
        public async Task<ActionResult<LedgerSummaryDto>> GetLedgerSummary(int customerId)
        {
            try
            {
                var summary = await _ledgerService.GetLedgerSummaryAsync(customerId);
                return Ok(summary);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve summary", message = ex.Message });
            }
        }

        /// <summary>
        /// Create manual ledger entry (Admin only)
        /// </summary>
        [HttpPost("entry")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CustomerLedgerDto>> CreateLedgerEntry([FromBody] CreateLedgerEntryDto dto)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var entry = await _ledgerService.CreateLedgerEntryAsync(dto, username);
                return CreatedAtAction(nameof(GetCustomerLedger), new { customerId = dto.CustomerId }, entry);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create entry", message = ex.Message });
            }
        }

        /// <summary>
        /// Record a payment (can be linked to invoice)
        /// </summary>
        [HttpPost("payment")]
        public async Task<ActionResult<object>> RecordPayment([FromBody] RecordPaymentDto dto)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                await _ledgerService.CreatePaymentLedgerEntryAsync(
                    dto.CustomerId,
                    dto.Amount,
                    dto.PaymentMethod,
                    dto.ReferenceNumber ?? "",
                    dto.InvoiceId,
                    username
                );

                var newBalance = await _ledgerService.GetCustomerBalanceAsync(dto.CustomerId);

                return Ok(new
                {
                    message = "Payment recorded successfully",
                    customerId = dto.CustomerId,
                    amount = dto.Amount,
                    newBalance
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to record payment", message = ex.Message });
            }
        }

        /// <summary>
        /// Get aging report - Customer Account Balance
        /// </summary>
        [HttpGet("aging-report")]
        public async Task<ActionResult<AgingReportDto>> GetAgingReport([FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var report = await _ledgerService.GetAgingReportAsync(asOfDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate aging report", message = ex.Message });
            }
        }

        /// <summary>
        /// Get aging report for specific customer
        /// </summary>
        [HttpGet("customer/{customerId}/aging")]
        public async Task<ActionResult<CustomerAgingDto>> GetCustomerAging(
            int customerId,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var aging = await _ledgerService.GetCustomerAgingAsync(customerId, asOfDate);
                return Ok(aging);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate aging report", message = ex.Message });
            }
        }

        /// <summary>
        /// Get all customers with outstanding balances
        /// </summary>
        [HttpGet("outstanding")]
        public async Task<ActionResult<List<CustomerAgingDto>>> GetOutstandingBalances()
        {
            try
            {
                var customers = await _ledgerService.GetCustomersWithOutstandingBalanceAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve outstanding balances", message = ex.Message });
            }
        }

        /// <summary>
        /// Get summaries for all customers
        /// </summary>
        [HttpGet("summaries")]
        public async Task<ActionResult<List<LedgerSummaryDto>>> GetAllSummaries()
        {
            try
            {
                var summaries = await _ledgerService.GetAllCustomerSummariesAsync();
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve summaries", message = ex.Message });
            }
        }
    }

    public class RecordPaymentDto
    {
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? ReferenceNumber { get; set; }
        public int? InvoiceId { get; set; }
    }
}

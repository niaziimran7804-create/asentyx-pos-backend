using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.DTOs;
using POS.Api.Services;
using System.Text;

namespace POS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoices(
            [FromQuery] decimal? minAmount,
            [FromQuery] decimal? maxAmount,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? customerAddress,
            [FromQuery] string? status)
        {
            // If any filter parameters are provided, use filtered endpoint
            if (minAmount.HasValue || maxAmount.HasValue || startDate.HasValue || endDate.HasValue ||
                !string.IsNullOrWhiteSpace(customerAddress) || !string.IsNullOrWhiteSpace(status))
            {
                var filter = new InvoiceFilterDto
                {
                    MinAmount = minAmount,
                    MaxAmount = maxAmount,
                    StartDate = startDate,
                    EndDate = endDate,
                    CustomerAddress = customerAddress,
                    Status = status
                };
                var invoices = await _invoiceService.GetFilteredInvoicesAsync(filter);
                return Ok(invoices);
            }

            // Otherwise return all invoices
            var allInvoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(allInvoices);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
                return NotFound();

            return Ok(invoice);
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<InvoiceDto>> GetInvoiceByOrderId(int orderId)
        {
            var invoice = await _invoiceService.GetInvoiceByOrderIdAsync(orderId);
            if (invoice == null)
                return NotFound();

            return Ok(invoice);
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> CreateInvoice([FromBody] CreateInvoiceDto createInvoiceDto)
        {
            try
            {
                var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto);
                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.InvoiceId }, invoice);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/print")]
        [AllowAnonymous] // Allow anonymous access for printing (invoices are meant to be shareable)
        public async Task<IActionResult> PrintInvoice(int id)
        {
            try
            {
                var html = await _invoiceService.GenerateInvoiceHtmlAsync(id);
                // Add print script to automatically trigger print dialog
                var htmlWithPrintScript = html + @"
                    <script>
                        window.onload = function() {
                            setTimeout(function() {
                                window.print();
                            }, 500);
                        };
                    </script>";
                return Content(htmlWithPrintScript, "text/html", Encoding.UTF8);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                    return NotFound();

                var html = await _invoiceService.GenerateInvoiceHtmlAsync(id);
                var bytes = Encoding.UTF8.GetBytes(html);
                return File(bytes, "text/html", $"Invoice_{invoice.InvoiceNumber}.html");
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("bulk-print")]
        [AllowAnonymous] // Allow anonymous access for printing
        public async Task<IActionResult> BulkPrintInvoices([FromQuery] string invoiceIds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(invoiceIds))
                    return BadRequest(new { message = "At least one invoice ID is required" });

                var ids = invoiceIds.Split(',')
                    .Select(id => int.TryParse(id.Trim(), out var result) ? result : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                if (ids.Count == 0)
                    return BadRequest(new { message = "Invalid invoice IDs provided" });

                var html = await _invoiceService.GenerateBulkInvoiceHtmlAsync(ids);
                // Add print script to automatically trigger print dialog
                var htmlWithPrintScript = html + @"
                    <script>
                        window.onload = function() {
                            setTimeout(function() {
                                window.print();
                            }, 1000);
                        };
                    </script>";
                return Content(htmlWithPrintScript, "text/html", Encoding.UTF8);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("shop-config")]
        public async Task<ActionResult<ShopConfigurationDto>> GetShopConfiguration()
        {
            var config = await _invoiceService.GetShopConfigurationAsync();
            return Ok(config);
        }

        [HttpPut("shop-config")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShopConfigurationDto>> UpdateShopConfiguration([FromBody] UpdateShopConfigurationDto dto)
        {
            var config = await _invoiceService.UpdateShopConfigurationAsync(dto);
            return Ok(config);
        }

        /// <summary>
        /// Add a payment to an invoice (partial or full)
        /// </summary>
        [HttpPost("{id}/payments")]
        public async Task<ActionResult<InvoicePaymentDto>> AddPayment(int id, [FromBody] CreateInvoicePaymentDto paymentDto)
        {
            try
            {
                var username = User.Identity?.Name ?? "System";
                var payment = await _invoiceService.AddPaymentAsync(id, paymentDto, username);
                return Ok(payment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get payment summary and history for an invoice
        /// </summary>
        [HttpGet("{id}/payments")]
        public async Task<ActionResult<InvoicePaymentSummaryDto>> GetInvoicePayments(int id)
        {
            try
            {
                var summary = await _invoiceService.GetInvoicePaymentsAsync(id);
                return Ok(summary);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all payment records for an invoice
        /// </summary>
        [HttpGet("{id}/payments/list")]
        public async Task<ActionResult<List<InvoicePaymentDto>>> GetAllPayments(int id)
        {
            var payments = await _invoiceService.GetAllPaymentsAsync(id);
            return Ok(payments);
        }

        /// <summary>
        /// Update invoice due date
        /// </summary>
        [HttpPut("{id}/due-date")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDueDate(int id, [FromBody] UpdateInvoiceDueDateDto dto)
        {
            try
            {
                var result = await _invoiceService.UpdateDueDateAsync(id, dto.DueDate);
                if (!result)
                    return NotFound(new { message = "Invoice not found" });

                return Ok(new { message = "Invoice due date updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create credit note invoice for a return
        /// </summary>
        [HttpPost("credit-note/return/{returnId}")]
        public async Task<ActionResult<CreditNoteDto>> CreateCreditNoteForReturn(int returnId)
        {
            try
            {
                var creditNote = await _invoiceService.CreateCreditNoteInvoiceAsync(returnId);
                return CreatedAtAction(nameof(GetCreditNoteByReturn), new { returnId }, creditNote);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create credit note", details = ex.Message });
            }
        }

        /// <summary>
        /// Get credit note by return ID
        /// </summary>
        [HttpGet("credit-note/return/{returnId}")]
        public async Task<ActionResult<CreditNoteDto>> GetCreditNoteByReturn(int returnId)
        {
            try
            {
                var creditNote = await _invoiceService.GetCreditNoteByReturnIdAsync(returnId);
                if (creditNote == null)
                    return NotFound(new { message = "Credit note not found for this return" });

                return Ok(creditNote);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve credit note", details = ex.Message });
            }
        }
    }
}


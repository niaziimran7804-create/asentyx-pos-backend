using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;
using POS.Api.Middleware;

namespace POS.Api.Services
{
    public class ReturnService : IReturnService
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductService _productService;
        private readonly IAccountingService _accountingService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<ReturnService> _logger;
        private readonly TenantContext _tenantContext;

        public ReturnService(
            ApplicationDbContext context,
            IProductService productService,
            IAccountingService accountingService,
            IInvoiceService invoiceService,
            ILogger<ReturnService> logger,
            TenantContext tenantContext)
        {
            _context = context;
            _productService = productService;
            _accountingService = accountingService;
            _invoiceService = invoiceService;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        public async Task<ReturnDto> CreateWholeReturnAsync(WholeReturnRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Validate invoice exists
                var invoice = await _context.Invoices
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Customer)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.OrderProductMaps)
                            .ThenInclude(opm => opm.Product)
                    .FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId);

                if (invoice == null)
                {
                    throw new InvalidOperationException($"Invoice with ID {request.InvoiceId} not found");
                }

                // 2. Check 14-day limit
                var daysDifference = (DateTime.UtcNow - invoice.InvoiceDate).TotalDays;
                if (daysDifference > 14)
                {
                    throw new InvalidOperationException("Invoice is older than 14 days and cannot be returned");
                }

                // 3. Validate return amount matches invoice total
                if (Math.Abs(request.TotalReturnAmount - invoice.TotalAmount) > 0.01m)
                {
                    throw new InvalidOperationException("Return amount must equal invoice total for whole returns");
                }

                // 4. Check if already returned
                var existingReturn = await _context.Returns
                    .FirstOrDefaultAsync(r => r.InvoiceId == request.InvoiceId && r.ReturnType == "whole");

                if (existingReturn != null)
                {
                    throw new InvalidOperationException("Invoice has already been fully returned");
                }

                // 5. Validate refund method
                var validMethods = new[] { "Cash", "Card", "Store Credit" };
                if (!validMethods.Contains(request.RefundMethod))
                {
                    throw new InvalidOperationException("Invalid refund method. Must be 'Cash', 'Card', or 'Store Credit'");
                }

                // 6. Enforce branch isolation
                if (invoice.Order!.BranchId != _tenantContext.BranchId)
                {
                    throw new InvalidOperationException("Cannot create return for invoice from different branch");
                }

                // 7. Create return record
                var returnEntity = new Return
                {
                    ReturnType = "whole",
                    InvoiceId = request.InvoiceId,
                    OrderId = request.OrderId,
                    ReturnDate = DateTime.UtcNow,
                    ReturnStatus = "Pending",
                    ReturnReason = request.ReturnReason,
                    RefundMethod = request.RefundMethod,
                    Notes = request.Notes,
                    TotalReturnAmount = request.TotalReturnAmount,
                    CompanyId = _tenantContext.CompanyId,
                    BranchId = _tenantContext.BranchId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Returns.Add(returnEntity);
                await _context.SaveChangesAsync();

                // 8. Restore inventory for all products in the order
                foreach (var orderProduct in invoice.Order!.OrderProductMaps)
                {
                    await _productService.RestoreInventoryAsync(orderProduct.ProductId, orderProduct.Quantity);
                    _logger.LogInformation(
                        "Inventory restored for product {ProductId}: +{Quantity} units",
                        orderProduct.ProductId, orderProduct.Quantity);
                }

                // 9. Create accounting entry
                var accountingEntry = new CreateAccountingEntryDto
                {
                    EntryType = "Refund",
                    Amount = request.TotalReturnAmount,
                    Description = $"Whole bill return - Invoice #{invoice.InvoiceNumber} | Return #{returnEntity.ReturnId}",
                    PaymentMethod = request.RefundMethod,
                    Category = "Sales Return - Whole Bill",
                    EntryDate = DateTime.UtcNow
                };

                await _accountingService.CreateAccountingEntryAsync(accountingEntry, "System");

                // 10. Auto-create credit note invoice for the return
                try
                {
                    var creditNote = await _invoiceService.CreateCreditNoteInvoiceAsync(returnEntity.ReturnId);
                    _logger.LogInformation(
                        "Credit note {CreditNoteNumber} automatically created for whole return {ReturnId}",
                        creditNote.CreditNoteNumber, returnEntity.ReturnId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "Failed to create credit note for return {ReturnId}. Can be created manually later.",
                        returnEntity.ReturnId);
                    // Don't fail the entire return if credit note creation fails
                }
                
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Whole bill return {ReturnId} created successfully for invoice {InvoiceId}",
                    returnEntity.ReturnId, request.InvoiceId);

                return await GetReturnByIdAsync(returnEntity.ReturnId) 
                    ?? throw new Exception("Failed to retrieve created return");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create whole return for invoice {InvoiceId}", request.InvoiceId);
                throw;
            }
        }

        public async Task<ReturnDto> CreatePartialReturnAsync(PartialReturnRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Validate invoice exists
                var invoice = await _context.Invoices
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Customer)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.OrderProductMaps)
                            .ThenInclude(opm => opm.Product)
                    .FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId);

                if (invoice == null)
                {
                    throw new InvalidOperationException($"Invoice with ID {request.InvoiceId} not found");
                }

                // 2. Check 14-day limit
                var daysDifference = (DateTime.UtcNow - invoice.InvoiceDate).TotalDays;
                if (daysDifference > 14)
                {
                    throw new InvalidOperationException("Invoice is older than 14 days and cannot be returned");
                }

                // 3. Validate items array
                if (request.Items.Count == 0)
                {
                    throw new InvalidOperationException("At least one product must be selected for return");
                }

                // 4. Validate refund method
                var validMethods = new[] { "Cash", "Card", "Store Credit" };
                if (!validMethods.Contains(request.RefundMethod))
                {
                    throw new InvalidOperationException("Invalid refund method. Must be 'Cash', 'Card', or 'Store Credit'");
                }

                // 5. Validate each return item
                decimal calculatedTotal = 0;
                foreach (var item in request.Items)
                {
                    // Check product belongs to order
                    var orderProduct = invoice.Order!.OrderProductMaps
                        .FirstOrDefault(opm => opm.ProductId == item.ProductId);

                    if (orderProduct == null)
                    {
                        throw new InvalidOperationException($"Product {item.ProductId} does not belong to this order");
                    }

                    // Check quantity limits
                    var previouslyReturned = await _context.ReturnItems
                        .Where(ri => ri.Return!.InvoiceId == request.InvoiceId && 
                                    ri.ProductId == item.ProductId)
                        .SumAsync(ri => ri.ReturnQuantity);

                    if (item.ReturnQuantity + previouslyReturned > orderProduct.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Return quantity for product {item.ProductId} exceeds ordered quantity " +
                            $"(ordered: {orderProduct.Quantity}, previously returned: {previouslyReturned}, " +
                            $"trying to return: {item.ReturnQuantity})");
                    }

                    // Validate amount calculation using the actual unit price from OrderProductMaps
                    var expectedAmount = orderProduct.UnitPrice * item.ReturnQuantity;
                    var difference = Math.Abs(item.ReturnAmount - expectedAmount);
                    
                    // Log detailed information for debugging
                    _logger.LogInformation(
                        "Validating return amount for Product {ProductId}: " +
                        "OrderProductMap UnitPrice={UnitPrice}, ReturnQuantity={Quantity}, " +
                        "Expected={Expected}, Received={Received}, Difference={Difference}",
                        item.ProductId, orderProduct.UnitPrice, item.ReturnQuantity, 
                        expectedAmount, item.ReturnAmount, difference);
                    
                    if (difference > 0.01m)
                    {
                        throw new InvalidOperationException(
                            $"Invalid return amount for product {item.ProductId}. " +
                            $"Expected: {expectedAmount:F2} (Unit Price: {orderProduct.UnitPrice:F2} × Quantity: {item.ReturnQuantity}), " +
                            $"Received: {item.ReturnAmount:F2}, " +
                            $"Difference: {difference:F2}");
                    }

                    calculatedTotal += item.ReturnAmount;
                }

                // 6. Validate total amount
                if (Math.Abs(request.TotalReturnAmount - calculatedTotal) > 0.01m)
                {
                    throw new InvalidOperationException(
                        $"Total return amount ({request.TotalReturnAmount}) does not match sum of item amounts ({calculatedTotal})");
                }

                // 7. Enforce branch isolation
                if (invoice.Order!.BranchId != _tenantContext.BranchId)
                {
                    throw new InvalidOperationException("Cannot create return for invoice from different branch");
                }

                // 8. Create return record
                var returnEntity = new Return
                {
                    ReturnType = "partial",
                    InvoiceId = request.InvoiceId,
                    OrderId = request.OrderId,
                    ReturnDate = DateTime.UtcNow,
                    ReturnStatus = "Pending",
                    ReturnReason = request.ReturnReason,
                    RefundMethod = request.RefundMethod,
                    Notes = request.Notes,
                    TotalReturnAmount = request.TotalReturnAmount,
                    CompanyId = _tenantContext.CompanyId,
                    BranchId = _tenantContext.BranchId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Returns.Add(returnEntity);
                await _context.SaveChangesAsync();

                // 9. Create return items and update inventory
                foreach (var item in request.Items)
                {
                    var returnItem = new ReturnItem
                    {
                        ReturnId = returnEntity.ReturnId,
                        ProductId = item.ProductId,
                        ReturnQuantity = item.ReturnQuantity,
                        ReturnAmount = item.ReturnAmount,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ReturnItems.Add(returnItem);
                    await _context.SaveChangesAsync();

                    // Restore inventory for each product
                    await _productService.RestoreInventoryAsync(item.ProductId, item.ReturnQuantity);
                    
                    _logger.LogInformation(
                        "Inventory restored for product {ProductId}: +{Quantity} units",
                        item.ProductId, item.ReturnQuantity);

                    // Create individual accounting entry per product
                    var product = invoice.Order!.OrderProductMaps
                        .First(opm => opm.ProductId == item.ProductId).Product;

                    var accountingEntry = new CreateAccountingEntryDto
                    {
                        EntryType = "Refund",
                        Amount = item.ReturnAmount,
                        Description = $"Partial return - {product?.ProductName} ({item.ReturnQuantity} units) | Return #{returnEntity.ReturnId}",
                        PaymentMethod = request.RefundMethod,
                        Category = "Sales Return - Partial",
                        EntryDate = DateTime.UtcNow
                    };

                    await _accountingService.CreateAccountingEntryAsync(accountingEntry, "System");
                }

                // 10. Auto-create credit note invoice for the partial return
                try
                {
                    var creditNote = await _invoiceService.CreateCreditNoteInvoiceAsync(returnEntity.ReturnId);
                    _logger.LogInformation(
                        "Credit note {CreditNoteNumber} automatically created for partial return {ReturnId}",
                        creditNote.CreditNoteNumber, returnEntity.ReturnId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "Failed to create credit note for return {ReturnId}. Can be created manually later.",
                        returnEntity.ReturnId);
                    // Don't fail the entire return if credit note creation fails
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Partial return {ReturnId} created successfully for invoice {InvoiceId} with {ItemCount} items",
                    returnEntity.ReturnId, request.InvoiceId, request.Items.Count);

                return await GetReturnByIdAsync(returnEntity.ReturnId)
                    ?? throw new Exception("Failed to retrieve created return");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create partial return for invoice {InvoiceId}", request.InvoiceId);
                throw;
            }
        }

        public async Task<IEnumerable<ReturnDto>> GetAllReturnsAsync()
        {
            // Enforce strict branch isolation - no branchId means no data
            if (!_tenantContext.BranchId.HasValue)
            {
                _logger.LogWarning("Attempted to retrieve returns without branch context");
                return Enumerable.Empty<ReturnDto>();
            }

            var returns = await _context.Returns
                .Where(r => r.BranchId == _tenantContext.BranchId.Value)
                .Include(r => r.Invoice)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Customer)
                .Include(r => r.ProcessedByUser)
                .Include(r => r.CreditNoteInvoice)
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.Product)
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} returns for branch {BranchId}", returns.Count, _tenantContext.BranchId.Value);
            return returns.Select(MapToDto);
        }

        public async Task<ReturnDto?> GetReturnByIdAsync(int id)
        {
            // Enforce strict branch isolation - no branchId means no data
            if (!_tenantContext.BranchId.HasValue)
            {
                _logger.LogWarning("Attempted to retrieve return {ReturnId} without branch context", id);
                return null;
            }

            var returnEntity = await _context.Returns
                .Where(r => r.ReturnId == id && r.BranchId == _tenantContext.BranchId.Value)
                .Include(r => r.Invoice)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Customer)
                .Include(r => r.ProcessedByUser)
                .Include(r => r.CreditNoteInvoice)
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.Product)
                .FirstOrDefaultAsync();

            if (returnEntity != null)
            {
                _logger.LogInformation("Retrieved return {ReturnId} from branch {BranchId}", id, _tenantContext.BranchId.Value);
            }
            else
            {
                _logger.LogWarning("Return {ReturnId} not found in branch {BranchId}", id, _tenantContext.BranchId.Value);
            }

            return returnEntity != null ? MapToDto(returnEntity) : null;
        }

        public async Task<ReturnSummaryDto> GetReturnSummaryAsync()
        {
            // Enforce strict branch isolation - no branchId means empty summary
            if (!_tenantContext.BranchId.HasValue)
            {
                _logger.LogWarning("Attempted to retrieve return summary without branch context");
                return new ReturnSummaryDto
                {
                    TotalReturns = 0,
                    PendingReturns = 0,
                    ApprovedReturns = 0,
                    CompletedReturns = 0,
                    TotalReturnAmount = 0,
                    WholeReturnsCount = 0,
                    PartialReturnsCount = 0
                };
            }

            var branchQuery = _context.Returns.Where(r => r.BranchId == _tenantContext.BranchId.Value);

            var summary = new ReturnSummaryDto
            {
                TotalReturns = await branchQuery.CountAsync(),
                PendingReturns = await branchQuery.CountAsync(r => r.ReturnStatus == "Pending"),
                ApprovedReturns = await branchQuery.CountAsync(r => r.ReturnStatus == "Approved"),
                CompletedReturns = await branchQuery.CountAsync(r => r.ReturnStatus == "Completed"),
                TotalReturnAmount = await branchQuery
                    .Where(r => r.ReturnStatus == "Completed")
                    .SumAsync(r => (decimal?)r.TotalReturnAmount) ?? 0,
                WholeReturnsCount = await branchQuery.CountAsync(r => r.ReturnType == "whole"),
                PartialReturnsCount = await branchQuery.CountAsync(r => r.ReturnType == "partial")
            };

            _logger.LogInformation("Return summary for branch {BranchId}: {Total} total, {Pending} pending, {Completed} completed",
                _tenantContext.BranchId.Value, summary.TotalReturns, summary.PendingReturns, summary.CompletedReturns);

            return summary;
        }

        public async Task<bool> UpdateReturnStatusAsync(int id, UpdateReturnStatusRequest request, int userId)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                _logger.LogWarning("Attempted to update return {ReturnId} status without branch context", id);
                return false;
            }

            var returnEntity = await _context.Returns
                .FirstOrDefaultAsync(r => r.ReturnId == id && r.BranchId == _tenantContext.BranchId.Value);
            
            if (returnEntity == null)
            {
                _logger.LogWarning("Return {ReturnId} not found in branch {BranchId}", id, _tenantContext.BranchId.Value);
                return false;
            }

            var validStatuses = new[] { "Pending", "Approved", "Completed", "Rejected" };
            if (!validStatuses.Contains(request.ReturnStatus))
            {
                throw new InvalidOperationException(
                    "Invalid return status. Must be 'Pending', 'Approved', 'Completed', or 'Rejected'");
            }

            var previousStatus = returnEntity.ReturnStatus;
            returnEntity.ReturnStatus = request.ReturnStatus;
            returnEntity.ProcessedBy = userId;
            returnEntity.ProcessedDate = DateTime.UtcNow;
            returnEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Return {ReturnId} status updated to {Status} by user {UserId}",
                id, request.ReturnStatus, userId);

            // Automatically create credit note invoice when status changes to "Completed"
            if (request.ReturnStatus == "Completed" && previousStatus != "Completed")
            {
                try
                {
                    // Check if credit note already exists
                    if (returnEntity.CreditNoteInvoiceId == null)
                    {
                        var creditNote = await _invoiceService.CreateCreditNoteInvoiceAsync(id);
                        
                        _logger.LogInformation(
                            "Credit note {CreditNoteNumber} automatically created for return {ReturnId}",
                            creditNote.CreditNoteNumber, id);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Credit note already exists for return {ReturnId}, skipping creation",
                            id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to automatically create credit note for return {ReturnId}. " +
                        "Credit note can be created manually later.", id);
                    // Don't throw - allow status update to succeed even if credit note creation fails
                    // Admin can manually create credit note later if needed
                }
            }

            return true;
        }

        private static ReturnDto MapToDto(Return returnEntity)
        {
            var dto = new ReturnDto
            {
                ReturnId = returnEntity.ReturnId,
                ReturnType = returnEntity.ReturnType,
                InvoiceId = returnEntity.InvoiceId,
                OrderId = returnEntity.OrderId,
                CreditNoteInvoiceId = returnEntity.CreditNoteInvoiceId,
                CreditNoteNumber = returnEntity.CreditNoteInvoice?.InvoiceNumber,
                ReturnDate = returnEntity.ReturnDate,
                ReturnStatus = returnEntity.ReturnStatus,
                TotalReturnAmount = returnEntity.TotalReturnAmount,
                RefundMethod = returnEntity.RefundMethod,
                ReturnReason = returnEntity.ReturnReason,
                Notes = returnEntity.Notes,
                CustomerFullName = returnEntity.Order?.Customer != null 
                    ? $"{returnEntity.Order.Customer.FirstName} {returnEntity.Order.Customer.LastName}" 
                    : null,
                CustomerPhone = returnEntity.Order?.Customer?.Phone,
                ProcessedBy = returnEntity.ProcessedBy,
                ProcessedByName = returnEntity.ProcessedByUser != null
                    ? $"{returnEntity.ProcessedByUser.FirstName} {returnEntity.ProcessedByUser.LastName}"
                    : null,
                ProcessedDate = returnEntity.ProcessedDate,
                ItemsCount = returnEntity.ReturnItems?.Count ?? 0,
                ReturnedItems = returnEntity.ReturnItems?.Select(ri => new ReturnedItemDto
                {
                    ProductId = ri.ProductId,
                    ProductName = ri.Product?.ProductName ?? "Unknown",
                    ReturnQuantity = ri.ReturnQuantity,
                    ReturnAmount = ri.ReturnAmount
                }).ToList(),
                Message = returnEntity.ReturnType == "whole"
                    ? "Whole bill return created successfully"
                    : $"Partial return created successfully with {returnEntity.ReturnItems?.Count ?? 0} items"
            };

            return dto;
        }
    }
}

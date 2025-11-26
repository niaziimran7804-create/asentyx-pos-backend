using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface ILedgerService
    {
        // Ledger Entries
        Task<CustomerLedgerDto> CreateLedgerEntryAsync(CreateLedgerEntryDto dto, string createdBy);
        Task<List<CustomerLedgerDto>> GetCustomerLedgerAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null);
        Task<CustomerStatementDto> GetCustomerStatementAsync(int customerId, DateTime startDate, DateTime endDate);
        Task<decimal> GetCustomerBalanceAsync(int customerId);
        Task<LedgerSummaryDto> GetLedgerSummaryAsync(int customerId);

        // Automatic ledger creation from transactions
        Task CreateSaleLedgerEntryAsync(int orderId, int invoiceId, string createdBy);
        Task CreatePaymentLedgerEntryAsync(int customerId, decimal amount, string paymentMethod, string referenceNumber, int? invoiceId, string createdBy);
        Task CreateRefundLedgerEntryAsync(int returnId, string createdBy);

        // Aging Reports
        Task<AgingReportDto> GetAgingReportAsync(DateTime? asOfDate = null);
        Task<CustomerAgingDto> GetCustomerAgingAsync(int customerId, DateTime? asOfDate = null);

        // Bulk operations
        Task<List<CustomerAgingDto>> GetCustomersWithOutstandingBalanceAsync();
        Task<List<LedgerSummaryDto>> GetAllCustomerSummariesAsync();
    }
}

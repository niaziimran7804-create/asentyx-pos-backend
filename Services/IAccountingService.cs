using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface IAccountingService
    {
        // Accounting Entries
        Task<AccountingEntriesResponseDto> GetAccountingEntriesAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? entryType = null,
            string? paymentMethod = null,
            string? category = null,
            int page = 1,
            int limit = 50);

        Task<AccountingEntryDto> CreateAccountingEntryAsync(CreateAccountingEntryDto dto, string createdBy);
        Task<bool> DeleteAccountingEntryAsync(int entryId);

        // Financial Summary
        Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Daily Sales
        Task<List<DailySalesDto>> GetDailySalesAsync(int days = 7);

        // Sales Graph
        Task<SalesGraphDto> GetSalesGraphAsync(DateTime startDate, DateTime endDate);

        // Payment Methods
        Task<List<PaymentMethodSummaryDto>> GetPaymentMethodsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Top Products
        Task<List<TopProductDto>> GetTopProductsAsync(int limit = 10, DateTime? startDate = null, DateTime? endDate = null);

        // Helper methods for automatic entry creation
        Task CreateSaleEntryFromOrderAsync(int orderId, string createdBy);
        Task CreateRefundEntryFromOrderAsync(int orderId, string createdBy);
        Task CreateExpenseEntryAsync(int expenseId, string createdBy);
    }
}

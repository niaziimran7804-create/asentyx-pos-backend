using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto);
        Task<InvoiceDto?> GetInvoiceByIdAsync(int id);
        Task<InvoiceDto?> GetInvoiceByOrderIdAsync(int orderId);
        Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync();
        Task<IEnumerable<InvoiceDto>> GetFilteredInvoicesAsync(InvoiceFilterDto filter);
        Task<string> GenerateInvoiceHtmlAsync(int invoiceId);
        Task<string> GenerateBulkInvoiceHtmlAsync(List<int> invoiceIds);
        Task<ShopConfigurationDto> GetShopConfigurationAsync();
        Task<ShopConfigurationDto> UpdateShopConfigurationAsync(UpdateShopConfigurationDto dto);
        Task<bool> UpdateInvoiceStatusByOrderIdAsync(int orderId, string status);
        
        // Partial payment methods
        Task<InvoicePaymentDto> AddPaymentAsync(int invoiceId, CreateInvoicePaymentDto paymentDto, string receivedBy);
        Task<InvoicePaymentSummaryDto> GetInvoicePaymentsAsync(int invoiceId);
        Task<List<InvoicePaymentDto>> GetAllPaymentsAsync(int invoiceId);
        
        // Update due date
        Task<bool> UpdateDueDateAsync(int invoiceId, DateTime dueDate);
    }
}


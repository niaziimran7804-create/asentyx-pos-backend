namespace POS.Api.Services
{
    public interface IEmailService
    {
        Task<bool> SendLowStockAlertAsync(string productName, decimal currentStock, int threshold);
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}

namespace POS.Api.Services
{
    public interface IEmailService
    {
        Task<bool> SendLowStockAlertAsync(string productName, int currentStock, int threshold);
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}

using Microsoft.Extensions.Options;
using POS.Api.Models;
using System.Net;
using System.Net.Mail;

namespace POS.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendLowStockAlertAsync(string productName, int currentStock, int threshold)
        {
            var subject = $"?? Low Stock Alert: {productName}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 5px; padding: 20px; margin: 20px 0;'>
                        <h2 style='color: #856404; margin-top: 0;'>?? Low Stock Alert</h2>
                        <p style='font-size: 16px; color: #333;'>
                            The following product has reached a low stock level:
                        </p>
                        <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold; width: 40%;'>Product Name:</td>
                                    <td style='padding: 8px;'>{productName}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Current Stock:</td>
                                    <td style='padding: 8px; color: #dc3545; font-weight: bold;'>{currentStock} units</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Threshold Level:</td>
                                    <td style='padding: 8px;'>{threshold} units</td>
                                </tr>
                            </table>
                        </div>
                        <p style='font-size: 14px; color: #856404; margin-bottom: 0;'>
                            <strong>Action Required:</strong> Please restock this product as soon as possible to avoid stock-out situations.
                        </p>
                    </div>
                    <p style='font-size: 12px; color: #6c757d; margin-top: 20px;'>
                        This is an automated notification from your POS Inventory Management System.
                    </p>
                </body>
                </html>
            ";

            return await SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                // Validate email settings
                if (string.IsNullOrEmpty(_emailSettings.SmtpServer) || 
                    string.IsNullOrEmpty(_emailSettings.SenderEmail) ||
                    string.IsNullOrEmpty(to))
                {
                    _logger.LogWarning("Email settings not configured or recipient email is missing");
                    return false;
                }

                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {to}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                return false;
            }
        }
    }
}

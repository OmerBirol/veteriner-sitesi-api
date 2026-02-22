using System.Net;
using System.Net.Mail;

namespace VetRandevu.Api.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var host = _configuration["Smtp:Host"];
        var portValue = _configuration["Smtp:Port"];
        var user = _configuration["Smtp:User"];
        var password = _configuration["Smtp:Password"];
        var fromName = _configuration["Smtp:FromName"];
        var fromEmail = _configuration["Smtp:FromEmail"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portValue) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP ayarlari eksik.");
        }

        if (!int.TryParse(portValue, out var port))
        {
            throw new InvalidOperationException("SMTP port gecersiz.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, string.IsNullOrWhiteSpace(fromName) ? fromEmail : fromName),
            Subject = subject,
            Body = body
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, password)
        };

        await client.SendMailAsync(message);
    }
}

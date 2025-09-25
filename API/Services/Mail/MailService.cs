using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

namespace API.Services.Mail;

public sealed class MailService : IMailService
{
    private readonly MailSettings _cfg;
    private readonly ILogger<MailService> _logger;
    private readonly IWebHostEnvironment _env;

    public MailService(IOptions<MailSettings> options, ILogger<MailService> logger, IWebHostEnvironment env)
    {
        _cfg = options.Value;
        _logger = logger;
        _env = env;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_cfg.FromName, _cfg.FromEmail));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;

        var body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody };
        msg.Body = body.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            try
            {
                // Try 587 with STARTTLS first
                await smtp.ConnectAsync(_cfg.SmtpServer, _cfg.SmtpPort, SecureSocketOptions.StartTls, ct);
            }
            catch
            {
                // Fallback to 465 (SSL on connect)
                await smtp.ConnectAsync(_cfg.SmtpServer, 465, SecureSocketOptions.SslOnConnect, ct);
            }

            await smtp.AuthenticateAsync(_cfg.SmtpUsername, _cfg.SmtpPassword, ct);
            await smtp.SendAsync(msg, ct);
            await smtp.DisconnectAsync(true, ct);

            _logger.LogInformation("📧 Mail sendt til {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved afsendelse til {To}", to);
            throw; // keep throwing so your test endpoint can show the exact error
        }
    }


    public Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken ct = default)
    {
        var html = LoadTemplate("Welcome.html")
            .Replace("{username}", WebUtility.HtmlEncode(username));

        var text = $"Hej {username}, din konto er oprettet. Velkommen til H2-MAGS!";

        return SendAsync(toEmail, "Velkommen til JoHotel", html, text, ct);
    }

    public Task SendBookingConfirmationEmailAsync(
        string toEmail, string username, string hotelName, string roomNumber,
        DateTime startDate, DateTime endDate, int numberOfGuests, decimal totalPrice, int bookingId,
        CancellationToken ct = default)
    {
        var html = LoadTemplate("BookingConfirmation.html")
            .Replace("{username}", WebUtility.HtmlEncode(username))
            .Replace("{hotelName}", WebUtility.HtmlEncode(hotelName))
            .Replace("{roomNumber}", WebUtility.HtmlEncode(roomNumber))
            .Replace("{startDate}", startDate.ToString("yyyy-MM-dd"))
            .Replace("{endDate}", endDate.ToString("yyyy-MM-dd"))
            .Replace("{numberOfGuests}", numberOfGuests.ToString())
            .Replace("{totalPrice}", totalPrice.ToString("C"))
            .Replace("{bookingId}", bookingId.ToString())
            .Replace("{currentYear}", DateTime.UtcNow.Year.ToString());


        var text =
            $"Hej {username}\n" +
            $"Din booking hos {hotelName} er bekræftet.\n" +
            $"Værelse #{roomNumber}\n" +
            $"Check-in: {startDate:yyyy-MM-dd}\n" +
            $"Check-out: {endDate:yyyy-MM-dd}\n" +
            $"Gæster: {numberOfGuests}\n" +
            $"Total pris: {totalPrice}\n" +
            $"Booking ID: {bookingId}";

        return SendAsync(toEmail, "Booking Bekræftelse", html, text, ct);
    }

    private string LoadTemplate(string fileName)
    {
        var path = Path.Combine(_env.ContentRootPath, "Services", "Mail", "Templates", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}

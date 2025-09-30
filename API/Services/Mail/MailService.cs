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
                await smtp.ConnectAsync(_cfg.SmtpServer, _cfg.SmtpPort, SecureSocketOptions.StartTls, ct);
            }
            catch
            {
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
            throw;
        }
    }

    public Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken ct = default)
    {
        var html = LoadTemplate("Welcome.html")
            .Replace("{username}", WebUtility.HtmlEncode(username));

        var text = $"Hej {username}, din konto er oprettet. Velkommen til!";
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
$@"Hej {username}
Din booking hos {hotelName} er bekræftet.
Værelse #{roomNumber}
Check-in: {startDate:yyyy-MM-dd}
Check-out: {endDate:yyyy-MM-dd}
Gæster: {numberOfGuests}
Total pris: {totalPrice}
Booking ID: {bookingId}";

        return SendAsync(toEmail, "Booking Bekræftelse", html, text, ct);
    }

    // ===== Ticket helpers =====
    public Task SendTicketCreatedUserAsync(string toEmail, string username, string number, string title, string deskUrl, string brand, CancellationToken ct = default)
    {
        var html = LoadTemplate("TicketCreatedUser.html")
            .Replace("{username}", WebUtility.HtmlEncode(username))
            .Replace("{ticketNumber}", WebUtility.HtmlEncode(number))
            .Replace("{title}", WebUtility.HtmlEncode(title))
            .Replace("{deskUrl}", WebUtility.HtmlEncode(deskUrl))
            .Replace("{brand}", WebUtility.HtmlEncode(brand))
            .Replace("{currentYear}", DateTime.UtcNow.Year.ToString());

        var text =
$@"Hej {username}

Din sag {number} er oprettet: {title}
Du kan følge den her: {deskUrl}

Vh {brand}";
        return SendAsync(toEmail, $"[{brand}] Sag oprettet {number}", html, text, ct);
    }

    public Task SendTicketCreatedStaffAsync(IEnumerable<string> toEmails, string number, string title, string customerName, string department, string deskLink, string brand, CancellationToken ct = default)
        => SendToManyAsync(toEmails, $"[{brand}] NY SAG {number} · {department}",
            LoadTemplate("TicketCreatedStaff.html")
                .Replace("{ticketNumber}", WebUtility.HtmlEncode(number))
                .Replace("{title}", WebUtility.HtmlEncode(title))
                .Replace("{customerName}", WebUtility.HtmlEncode(customerName))
                .Replace("{department}", WebUtility.HtmlEncode(department))
                .Replace("{deskUrl}", WebUtility.HtmlEncode(deskLink))
                .Replace("{brand}", WebUtility.HtmlEncode(brand))
                .Replace("{currentYear}", DateTime.UtcNow.Year.ToString()),
            $@"Ny sag {number} ({department}) fra {customerName}: {title}
Åbn: {deskLink}", ct);

    public Task SendTicketReplyToUserAsync(string toEmail, string username, string number, string messagePreview, string deskLink, string brand, CancellationToken ct = default)
    {
        var html = LoadTemplate("TicketReplyUser.html")
            .Replace("{username}", WebUtility.HtmlEncode(username))
            .Replace("{ticketNumber}", WebUtility.HtmlEncode(number))
            .Replace("{message}", WebUtility.HtmlEncode(messagePreview))
            .Replace("{deskUrl}", WebUtility.HtmlEncode(deskLink))
            .Replace("{brand}", WebUtility.HtmlEncode(brand))
            .Replace("{currentYear}", DateTime.UtcNow.Year.ToString());

        var text =
$@"Hej {username}

Der er kommet svar på din sag {number}:
{messagePreview}

Se og svar her: {deskLink}";
        return SendAsync(toEmail, $"[{brand}] Nyt svar på sag {number}", html, text, ct);
    }

    public Task SendTicketReplyToStaffAsync(IEnumerable<string> toEmails, string number, string customerName, string messagePreview, string deskLink, string brand, CancellationToken ct = default)
        => SendToManyAsync(toEmails, $"[{brand}] Nyt kundesvar · {number}",
            LoadTemplate("TicketReplyStaff.html")
                .Replace("{ticketNumber}", WebUtility.HtmlEncode(number))
                .Replace("{customerName}", WebUtility.HtmlEncode(customerName))
                .Replace("{message}", WebUtility.HtmlEncode(messagePreview))
                .Replace("{deskUrl}", WebUtility.HtmlEncode(deskLink))
                .Replace("{brand}", WebUtility.HtmlEncode(brand))
                .Replace("{currentYear}", DateTime.UtcNow.Year.ToString()),
            $@"Kunden {customerName} har svaret på sag {number}:
{messagePreview}

Åbn: {deskLink}", ct);

    private async Task SendToManyAsync(IEnumerable<string> toEmails, string subject, string html, string text, CancellationToken ct)
    {
        foreach (var to in toEmails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try { await SendAsync(to, subject, html, text, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Failed sending staff mail to {To}", to); }
        }
    }

    private string LoadTemplate(string fileName)
    {
        var path = Path.Combine(_env.ContentRootPath, "Services", "Mail", "Templates", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

namespace API.Services.Mail;

// Mail service der kan sende forskellige mails
public sealed class MailService : IMailService
{
    // Konfiguration til mail (SMTP mm.)
    private readonly MailSettings _cfg;
    // Logger så vi kan se fejl i loggen
    private readonly ILogger<MailService> _logger;
    // Bruges til at finde filstier i projektet
    private readonly IWebHostEnvironment _env;

    // Får konfiguration, logger og miljø ind via dependency injection
    public MailService(IOptions<MailSettings> options, ILogger<MailService> logger, IWebHostEnvironment env)
    {
        _cfg = options.Value;
        _logger = logger;
        _env = env;
    }

    // Generel metode til at sende en mail
    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        // Opbygger selve mailen 
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_cfg.FromName, _cfg.FromEmail));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;

        // Sætter indhold (HTML og tekst)
        var body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody };
        msg.Body = body.ToMessageBody();

        // SMTP klient til at sende mailen
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

    // Velkomstmail
    public Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken ct = default)
    {
        
        var html = LoadTemplate("Welcome.html").Replace("{username}", WebUtility.HtmlEncode(username));
        
        var text = $"Hej {username}, din konto er oprettet. Velkommen til H2-MAGS!";
        
        return SendAsync(toEmail, "Velkommen til JoHotel", html, text, ct);
    }

    // Booking konfirmation til kunden
    public Task SendBookingConfirmationEmailAsync(
        string toEmail, string username, string hotelName, string roomNumber,
        DateTime startDate, DateTime endDate, int numberOfGuests, decimal totalPrice, int bookingId,
        CancellationToken ct = default)
    {
        // Indsætter data (HTML) i skabelon
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

        // Tekstversion, i tilfælde af HTML ikke er understøttet i pågældende mail klient
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

    // Mail til bruger der har oprettet en ticket
    public Task SendTicketCreatedUserAsync(string toEmail, string username, string number, string title, string linkUrl, string brand, CancellationToken ct = default)
    {
        var html = LoadTemplate("TicketCreatedUser.html")
            .Replace("{username}", WebUtility.HtmlEncode(username))
            .Replace("{ticketNumber}", WebUtility.HtmlEncode(number))
            .Replace("{title}", WebUtility.HtmlEncode(title))
            .Replace("{deskUrl}", WebUtility.HtmlEncode(linkUrl))
            .Replace("{brand}", WebUtility.HtmlEncode(brand))
            .Replace("{currentYear}", DateTime.UtcNow.Year.ToString());

        var text =
$@"Hej {username}

Din sag {number} er oprettet: {title}
Du kan følge den her: {linkUrl}

Vh {brand}";

        return SendAsync(toEmail, $"[{brand}] Sag oprettet {number}", html, text, ct);
    }

    // Mail til staff når en ny ticket kommer ind af døren og skal besvareres
    public Task SendTicketCreatedStaffAsync(IEnumerable<string> toEmails, string number, string title, string customerName, string department, string linkUrl, string brand, CancellationToken ct = default)
        => SendToManyAsync(
            toEmails,
            $"[{brand}] NY SAG {number} · {department}",
            // HTML version af mail
            LoadTemplate("TicketCreatedStaff.html")
                .Replace("{ticketNumber}", WebUtility.HtmlEncode(number))
                .Replace("{title}", WebUtility.HtmlEncode(title))
                .Replace("{customerName}", WebUtility.HtmlEncode(customerName))
                .Replace("{department}", WebUtility.HtmlEncode(department))
                .Replace("{deskUrl}", WebUtility.HtmlEncode(linkUrl))
                .Replace("{brand}", WebUtility.HtmlEncode(brand))
                .Replace("{currentYear}", DateTime.UtcNow.Year.ToString()),
            // Tekstversion af mail
            $@"Ny sag {number} ({department}) fra {customerName}: {title}
Åbn: {linkUrl}",
            ct
        );

    // Mail til bruger når der er kommet svar på ticket
    public Task SendTicketReplyToUserAsync(string toEmail, string username, string number, string messagePreview, string linkUrl, string brand, CancellationToken ct = default)
    {
        var html = LoadTemplate("TicketReplyUser.html")
            .Replace("{username}", WebUtility.HtmlEncode(username))
            .Replace("{ticketNumber}", WebUtility.HtmlEncode(number))
            .Replace("{message}", WebUtility.HtmlEncode(messagePreview))
            .Replace("{deskUrl}", WebUtility.HtmlEncode(linkUrl))
            .Replace("{brand}", WebUtility.HtmlEncode(brand))
            .Replace("{currentYear}", DateTime.UtcNow.Year.ToString());

        var text =
$@"Hej {username}

Der er kommet svar på din sag {number}:
{messagePreview}

Se og svar her: {linkUrl}";

        return SendAsync(toEmail, $"[{brand}] Nyt svar på sag {number}", html, text, ct);
    }

    // Mail til medarbejder når kunden har svaret
    public Task SendTicketReplyToStaffAsync(IEnumerable<string> toEmails, string number, string customerName, string messagePreview, string linkUrl, string brand, CancellationToken ct = default)
        => SendToManyAsync(
            toEmails,
            $"[{brand}] Nyt kundesvar · {number}",
            LoadTemplate("TicketReplyStaff.html")
                .Replace("{ticketNumber}", WebUtility.HtmlEncode(number))
                .Replace("{customerName}", WebUtility.HtmlEncode(customerName))
                .Replace("{message}", WebUtility.HtmlEncode(messagePreview))
                .Replace("{deskUrl}", WebUtility.HtmlEncode(linkUrl))
                .Replace("{brand}", WebUtility.HtmlEncode(brand))
                .Replace("{currentYear}", DateTime.UtcNow.Year.ToString()),
            $@"Kunden {customerName} har svaret på sag {number}:
{messagePreview}

Åbn: {linkUrl}",
            ct
        );

    // Hjælpemetode der sender samme mail til flere adresser
    private async Task SendToManyAsync(IEnumerable<string> toEmails, string subject, string html, string text, CancellationToken ct)
    {
        // Undgår vi sender samme mail flere gange til samme adresse
        foreach (var to in toEmails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await SendAsync(to, subject, html, text, ct);
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "Failed sending staff mail to {To}", to);
            }
        }
    }

    // Ind læser en HTML-skabelon
    private string LoadTemplate(string fileName)
    {
        
        var path = Path.Combine(_env.ContentRootPath, "Services", "Mail", "Templates", fileName);

        
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}

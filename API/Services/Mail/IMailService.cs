namespace API.Services.Mail;

public interface IMailService
{
    Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);

    Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken ct = default);
    Task SendBookingConfirmationEmailAsync(
        string toEmail, string username, string hotelName, string roomNumber,
        DateTime startDate, DateTime endDate, int numberOfGuests, decimal totalPrice, int bookingId,
        CancellationToken ct = default);


    Task SendTicketCreatedUserAsync(string toEmail, string username, string number, string title, string linkUrl, string brand, CancellationToken ct = default);
    Task SendTicketCreatedStaffAsync(IEnumerable<string> toEmails, string number, string title, string customerName, string department, string linkUrl, string brand, CancellationToken ct = default);
    Task SendTicketReplyToUserAsync(string toEmail, string username, string number, string messagePreview, string linkUrl, string brand, CancellationToken ct = default);
    Task SendTicketReplyToStaffAsync(IEnumerable<string> toEmails, string number, string customerName, string messagePreview, string linkUrl, string brand, CancellationToken ct = default);
}

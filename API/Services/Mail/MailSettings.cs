namespace API.Services.Mail;

public sealed class MailSettings
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = default!;
    public string SmtpPassword { get; set; } = default!;
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = "JoHotel";
}

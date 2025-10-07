namespace DomainModels
{
    public enum TicketStatus { Ny, Open, PendingCustomer, PendingInternal, Resolved, Closed, Canceled }
    public enum TicketPriority { Low, Normal, High, Critical }

    public class Ticket : Common
    {
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public int CustomerUserId { get; set; }
        public User CustomerUser { get; set; } = default!;

        public int? AssigneeUserId { get; set; }
        public User? AssigneeUser { get; set; }

        public required string Number { get; set; }         
        public required string Title { get; set; }
        public string Department { get; set; } = "Reception";
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;
        public TicketStatus Status { get; set; } = TicketStatus.Ny;

        public DateTimeOffset? FirstResponseAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public DateTimeOffset? FirstResponseDueAt { get; set; }
        public DateTimeOffset? ResolveDueAt { get; set; }
        public string? ResolutionNote { get; set; }

        public List<TicketMessage> Messages { get; set; } = new();
        public List<TicketAttachment> Attachments { get; set; } = new();
    }

    public class TicketMessage : Common
    {
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = default!;

        public int AuthorUserId { get; set; }
        public User AuthorUser { get; set; } = default!;

        public required string AuthorName { get; set; }
        public bool IsInternal { get; set; }
        public required string Content { get; set; }
    }

    public class TicketAttachment : Common
    {
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = default!;

        public required string FileName { get; set; }
        public required string ContentType { get; set; }
        public required string StoragePath { get; set; }
        public long Size { get; set; }
        public int UploadedByUserId { get; set; }
    }
}

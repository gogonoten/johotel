namespace DomainModels
{
    public class CreateTicketDto
    {
        public int? BookingId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public string Department { get; set; } = "Reception";
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    }

    public class TicketDto
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";
        public string Title { get; set; } = "";
        public int? BookingId { get; set; }
        public string Department { get; set; } = "";
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class TicketDetailDto : TicketDto
    {
        public required SimpleUserDto Customer { get; set; }
        public List<RoomBookingDto> Bookings { get; set; } = new();
        public List<TicketMessageDto> Messages { get; set; } = new();
    }

    public class SimpleUserDto { public int Id { get; set; } public string Email { get; set; } = ""; public string Username { get; set; } = ""; public string PhoneNumber { get; set; } = ""; }
    public class RoomBookingDto { public int Id { get; set; } public int RoomNumber { get; set; } public RoomType RoomType { get; set; } public DateTimeOffset CheckIn { get; set; } public DateTimeOffset CheckOut { get; set; } }

    public class TicketMessageDto { public int Id { get; set; } public string AuthorName { get; set; } = ""; public bool IsInternal { get; set; } public string Content { get; set; } = ""; public DateTimeOffset CreatedAt { get; set; } }

    public class PostTicketMessageDto { public required string Content { get; set; } public bool IsInternal { get; set; } }
}

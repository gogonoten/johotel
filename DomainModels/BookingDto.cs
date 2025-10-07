using System.ComponentModel.DataAnnotations;

namespace DomainModels;

public class BookingDto
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTimeOffset CheckIn { get; set; }

    [Required]
    public DateTimeOffset CheckOut { get; set; }
}

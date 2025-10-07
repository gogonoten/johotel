using System.ComponentModel.DataAnnotations;

namespace DomainModels;

public class Booking : Common
{ 
    [Required]
    public int UserId { get; set; }
    public User User { get; set; } = default!;

    [Required]
    public int RoomId { get; set; }
    public Room Room { get; set; } = default!;

    [Required]
    public DateTimeOffset CheckIn { get; set; }

    [Required]
    public DateTimeOffset CheckOut { get; set; }

    public bool IsConfirmed { get; set; } = false;
}


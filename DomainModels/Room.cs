using System.ComponentModel.DataAnnotations;

namespace DomainModels
{
    public class Room : Common
    {
        [Required]
        public int RoomNumber { get; set; }

        [Required]
        public RoomType Type { get; set; } = RoomType.Standard;

        public bool IsAvailable { get; set; } = true;

        public List<Booking> Bookings { get; set; } = new();
    }
}

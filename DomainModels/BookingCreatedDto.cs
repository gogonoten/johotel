namespace DomainModels
{
    public class BookingCreatedDto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int RoomNumber { get; set; }
        public RoomType RoomType { get; set; }
        public DateTimeOffset CheckIn { get; set; }
        public DateTimeOffset CheckOut { get; set; }
        public int Nights { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

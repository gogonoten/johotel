namespace DomainModels
{
    public class RoomDto
    {
        public int Id { get; set; }
        public int RoomNumber { get; set; }
        public RoomType Type { get; set; }
        public bool IsAvailable { get; set; }
    }
}

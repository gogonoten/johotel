using API.Repositories;
using DomainModels;
using OneOf;
using API.BookingService;
using OneOf.Types;

namespace API.Services
{
    /// <summary>
    /// Simpelt booking-service.
    /// Tager imod ønsker om at hente, oprette og aflyse bookinger
    /// og snakker sammen med repos/db.
    /// </summary>
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repo;

        // Vi får repo ind udefra 
        public BookingService(IBookingRepository repo) => _repo = repo;

        /// <summary>
        /// Henter alle bookinger for en bestemt bruger.
        /// Returnerer et objekt/booking med det mest nødvendige.
        /// </summary>
        public async Task<IReadOnlyList<object>> GetBookingsForUserAsync(int userId)
        {
            
            var list = await _repo.GetByUserWithRoomAsync(userId);

            
            return list.Select(b => new
            {
                b.Id,
                b.RoomId,
                RoomNumber = b.Room.RoomNumber,
                b.CheckIn,
                b.CheckOut,
                b.IsConfirmed,
                
                TotalPrice = Pricing.PriceForStay(b.Room.Type, b.CheckIn, b.CheckOut) ?? 0m
            } as object).ToList();
        }

        /// <summary>
        /// Hent alle bookinger.
        /// </summary>
        public async Task<IReadOnlyList<object>> GetAllAsync()
        {
            var list = await _repo.GetAllWithUserAndRoomAsync();

            // Mapper til objekter der er nemme at sende ud via API
            return list.Select(b => new
            {
                b.Id,
                User = new { b.User.Id, b.User.Email, b.User.Username },
                Room = new { b.Room.Id, b.Room.RoomNumber },
                b.CheckIn,
                b.CheckOut,
                b.IsConfirmed,
                TotalPrice = Pricing.PriceForStay(b.Room.Type, b.CheckIn, b.CheckOut) ?? 0m
            } as object).ToList();
        }

        /// <summary>
        /// Opretter en booking. Tjekker om værelset findes og om der evt er overlap.
        /// Returnerer enten et objekt med sucess eller en Bookingfejl.
        /// </summary>
        public async Task<OneOf<object, BookingError>> CreateAsync(int userId, BookingDto dto)
        {
            
            var utcCheckIn = dto.CheckIn.ToUniversalTime();
            var utcCheckOut = dto.CheckOut.ToUniversalTime();

            // Validering
            if (!await _repo.RoomExistsAsync(dto.RoomId)) return BookingError.NotFound;           
            if (await _repo.HasOverlapAsync(dto.RoomId, utcCheckIn, utcCheckOut)) return BookingError.Overlap; 

            // Værelses info ; type værelse og #
            var room = await _repo.GetRoomAsync(dto.RoomId);
            if (room is null) return BookingError.NotFound;

            // Udregner pris og antal nætter
            var nights = (utcCheckOut.Date - utcCheckIn.Date).Days;
            var totalPrice = Pricing.PriceForStay(room.Type, utcCheckIn, utcCheckOut) ?? 0m;

            // Laver booking objektet
            var now = DateTimeOffset.UtcNow;
            var booking = new Booking
            {
                UserId = userId,
                RoomId = dto.RoomId,
                CheckIn = utcCheckIn,
                CheckOut = utcCheckOut,
                CreatedAt = now,
                UpdatedAt = now,
                IsConfirmed = true 
            };

            
            await _repo.AddAsync(booking);
            await _repo.SaveChangesAsync();

            // Returnerer noget til controller og e-mail kan bruge
            return new
            {
                message = "Booking oprettet!",
                booking.Id,
                booking.RoomId,
                RoomNumber = room.RoomNumber,
                RoomType = room.Type,
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                Nights = nights,
                NumberOfGuests = 1, 
                HotelName = "JoHotel",
                TotalPrice = totalPrice
            };
        }

        /// <summary>
        /// Aflys en booking hvis det er brugerens egen og der er mere end 24 timer til check-in.
        /// Returnerer Success eller melder Booking fejl.
        /// </summary>
        public async Task<OneOf<Success, BookingError>> CancelAsync(int userId, int bookingId)
        {
            
            var booking = await _repo.GetByIdAsync(bookingId, asNoTracking: false);
            if (booking is null) return BookingError.NotFound;          

            if (booking.UserId != userId) return BookingError.Forbidden; 

            
            if (booking.CheckIn <= DateTimeOffset.UtcNow.AddHours(24))
                return BookingError.TooLate;

            
            _repo.Remove(booking);
            await _repo.SaveChangesAsync();
            return new Success();
        }
    }
}

using DomainModels;
using OneOf;
using OneOf.Types;
using System.Threading.Tasks;

namespace API.BookingService
{
    public enum BookingError { NotFound, Overlap, Forbidden, TooLate, Unknown }

    public interface IBookingService
    {
        Task<IReadOnlyList<object>> GetBookingsForUserAsync(int userId);
        Task<IReadOnlyList<object>> GetAllAsync();
        Task<OneOf.OneOf<object, BookingError>> CreateAsync(int userId, BookingDto dto);
        Task<OneOf<Success, BookingError>> CancelAsync(int userId, int bookingId);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API.Services;
using DomainModels;
using API.BookingService;
using API.Services.Mail;
using API.Data;

namespace API.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _service;
        private readonly IMailService _mail;

        public BookingsController(IBookingService service, IMailService mail)
        {
            _service = service;
            _mail = mail;
        }

        // Hent mine bookinger. 401 hvis ikke logget ind
        [HttpGet("my")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var bookings = await _service.GetBookingsForUserAsync(userId.Value);
            return Ok(bookings);
        }

        // Opret booking. Validerer dato. Kalder service, og sender bekræftelsesmail 
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateBooking([FromBody] BookingDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var utcDto = new BookingDto
            {
                RoomId = dto.RoomId,
                CheckIn = dto.CheckIn.ToUniversalTime(),
                CheckOut = dto.CheckOut.ToUniversalTime()
            };

            // Dato validering
            if (utcDto.CheckIn >= utcDto.CheckOut)
                return BadRequest(new { message = "CheckIn must be before CheckOut." });

            if (utcDto.CheckOut <= DateTimeOffset.UtcNow)
                return BadRequest(new { message = "CheckOut must be in the future." });

            // Service - laver overlap og opretter
            var result = await _service.CreateAsync(userId.Value, utcDto);

            if (result.TryPickT0(out var b, out var err))
            {
                // Henter mail og navn fra token clamis
                var toEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
                var username = User.Identity?.Name ?? "Guest";

                // Læs felter ud af b 
                string hotelName = GetProp(b, "HotelName", "Hotel");
                RoomType? roomType = GetProp<RoomType?>(b, "RoomType", null);
                string roomDisplay = roomType.HasValue ? RoomHelpers.DisplayName(roomType.Value) : "Ukendt værelse";

                int guests = GetProp(b, "NumberOfGuests", 1);
                decimal totalPrice = GetProp(b, "TotalPrice", 0m);
                int bookingId = GetProp(b, "Id", 0);

                // Brug værdier fra 'b' hvis de findes, ellers dem vi lige sendte
                DateTime startDate = GetProp<DateTimeOffset?>(b, "CheckIn", null)?.UtcDateTime
                                     ?? utcDto.CheckIn.UtcDateTime;
                DateTime endDate = GetProp<DateTimeOffset?>(b, "CheckOut", null)?.UtcDateTime
                                     ?? utcDto.CheckOut.UtcDateTime;

                // Sender e-mail, hele requested fejler ikke selvom mail skulle fejle
                if (!string.IsNullOrWhiteSpace(toEmail))
                {
                    try
                    {
                        await _mail.SendBookingConfirmationEmailAsync(
                            toEmail: toEmail,
                            username: username,
                            hotelName: hotelName,
                            roomNumber: roomDisplay, 
                            startDate: startDate,
                            endDate: endDate,
                            numberOfGuests: guests,
                            totalPrice: totalPrice,
                            bookingId: bookingId
                        );
                    }
                    catch
                    {
                        
                    }
                }

                return Ok(b);
            }

            
            return err switch
            {
                BookingError.NotFound => NotFound(new { message = "Værelset findes ikke." }),
                BookingError.Overlap => BadRequest(new { message = "Værelset er allerede booket i denne periode." }),
                _ => BadRequest(new { message = "Kunne ikke oprette booking." })
            };
        }

        // Helper: læs en egenskab ud hvis den findes 
        private static T GetProp<T>(object? src, string name, T fallback)
        {
            if (src is null) return fallback;
            var pi = src.GetType().GetProperty(name);
            if (pi is null) return fallback;

            var val = pi.GetValue(src);
            if (val is null) return fallback;

            if (val is T ok) return ok; 

            try
            {
                var targetType = typeof(T);
                var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return (T)Convert.ChangeType(val, underlying); 
            }
            catch
            {
                return fallback; 
            }
        }

        // Admin/Manager kan liste alle bookigner
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllBookings()
        {
            var bookings = await _service.GetAllAsync();
            return Ok(bookings);
        }

        // Annullerer booking for indlogget bruger (service tjekker 24-timers regel)
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _service.CancelAsync(userId.Value, id);
            return result.Match<IActionResult>(
                _ => NoContent(),
                err => err switch
                {
                    BookingError.NotFound => NotFound(new { message = "Bookingen findes ikke." }),
                    BookingError.Forbidden => Forbid(),
                    BookingError.TooLate => BadRequest(new { message = "Det er for sent at annullere denne booking." }),
                    _ => BadRequest(new { message = "Kunne ikke annullere bookingen." })
                }
            );
        }

        // Får bruger id fra JWT
        private int? GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }
    }
}

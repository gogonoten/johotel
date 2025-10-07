using Microsoft.AspNetCore.Mvc;
using API.Data;
using DomainModels;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly AppDBContext _db;
        public RoomsController(AppDBContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var fromUtc = (from ?? nowUtc).ToUniversalTime();
            var toUtc = (to ?? nowUtc.AddDays(1)).ToUniversalTime();

            var rooms = await _db.Rooms
                .AsNoTracking()
                .OrderBy(r => r.RoomNumber)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    Type = r.Type,
                    IsAvailable = !_db.Bookings.Any(b =>
                        b.RoomId == r.Id &&
                        b.CheckIn < toUtc &&
                        b.CheckOut > fromUtc)
                })
                .ToListAsync();

            return Ok(rooms);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var fromUtc = (from ?? nowUtc).ToUniversalTime();
            var toUtc = (to ?? nowUtc.AddDays(1)).ToUniversalTime();

            var room = await _db.Rooms
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    Type = r.Type,
                    IsAvailable = !_db.Bookings.Any(b =>
                        b.RoomId == r.Id &&
                        b.CheckIn < toUtc &&
                        b.CheckOut > fromUtc)
                })
                .FirstOrDefaultAsync();

            if (room is null) return NotFound();
            return Ok(room);
        }

        /// <summary>
        /// Returnere bekræftede bookinger for værelse der er indenfor dato range
        /// </summary>
        [HttpGet("{id:int}/booked")]
        public async Task<IActionResult> GetBookedSpans(
            int id,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to)
        {
            var start = (from ?? DateTimeOffset.UtcNow.AddDays(-30)).ToUniversalTime();
            var end = (to ?? DateTimeOffset.UtcNow.AddDays(120)).ToUniversalTime();

            var data = await _db.Bookings.AsNoTracking()
                .Where(b => b.RoomId == id && b.IsConfirmed && b.CheckIn < end && b.CheckOut > start)
                .Select(b => new { b.CheckIn, b.CheckOut })
                .OrderBy(b => b.CheckIn)
                .ToListAsync();

            return Ok(data);
        }
    }
}

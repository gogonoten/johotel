using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Services;

namespace API.Controllers
{
    /// <summary>Personale login med LDAP - Returner en JWT.</summary>
    [ApiController]
    [Route("api/staff-auth")]
    public sealed class StaffAuthController : ControllerBase
    {
        private readonly ILdapService _ldap;
        private readonly JwtService _jwt;

        public StaffAuthController(ILdapService ldap, JwtService jwt)
        {
            _ldap = ldap;
            _jwt = jwt;
        }

        // Login DTO 
        public sealed class StaffLoginDto
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }

        /// <summary> LDAP login og  JWT.</summary>
        /// <returns>200 med token, ellers 401.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] StaffLoginDto dto)
        {
            var (ok, userName, roles, error) = await _ldap.AuthenticateAsync(dto.Username, dto.Password);
            if (!ok || userName is null)
                return Unauthorized(new { message = error ?? "Unauthorized" });

            var token = _jwt.GenerateSecurityToken(userName, roles);
            return Ok(new { token, user = userName, roles });
        }
    }
}

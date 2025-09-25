using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq; 
using DomainModels;

namespace API.Services
{
    // JWT-service: opretter og signer tokens (DB-brugere og AD-personale)
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtService(IConfiguration configuration)
        {
            // Læser appsettings.json og miljøvariabler

            _secretKey = configuration["Jwt:SecretKey"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? "SuperSecretKey12345678910111213141516asdfghjkl!?JakobJensen";

            _issuer = configuration["Jwt:Issuer"]
                ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? "MyHotelApi";

            _audience = configuration["Jwt:Audience"]
                ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? "MyHotelFrontend";

            // Levetid i minutter 

            _expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"]
                ?? Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES")
                ?? "60");
        }


        // Token til DB-bruger bestående af id, navn, email og rolle

        public string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("email", user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("username", user.Username),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            if (user.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
                claims.Add(new Claim("role", user.Role.Name));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // --------------------- Tilføjet fra FIL 1 ---------------------

        // Token når vi kun kender brugernavn og rolle (til personale over AD)
        public string GenerateSecurityToken(string userName, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
                new Claim(ClaimTypes.Name, userName),
                new Claim("username", userName)
            };

            AddRoleClaims(claims, roles);
            return WriteToken(claims);
        }

        // Token med valgfri User og roller (kan bruges både til DB-bruger og personale)
        public string GenerateSecurityToken(User? user, IEnumerable<string> roles)
        {
            var claims = new List<Claim>();

            if (user is not null)
            {
                if (user.Id != 0)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    claims.Add(new Claim("userId", user.Id.ToString()));
                }

                if (!string.IsNullOrWhiteSpace(user.Username))
                {
                    claims.Add(new Claim(ClaimTypes.Name, user.Username));
                    claims.Add(new Claim("username", user.Username));
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, user.Email));
                    claims.Add(new Claim("email", user.Email));
                }
            }

            AddRoleClaims(claims, roles);
            return WriteToken(claims);
        }

        // Tilføjer rolle-claims (både standard og custom key)
        private void AddRoleClaims(List<Claim> claims, IEnumerable<string> roles)
        {
            foreach (var r in roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(r)) continue;
                claims.Add(new Claim(ClaimTypes.Role, r));
                claims.Add(new Claim("role", r));
            }
        }

        // Bygger og signerer JWT (genbruges af de ekstra overloads)
        private string WriteToken(IEnumerable<Claim> claims)
        {
            var keyBytes = Encoding.ASCII.GetBytes(_secretKey);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

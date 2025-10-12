using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using DomainModels;

namespace API.Services
{
    // Opretter og signer tokens til kunder (neon brugere) og staff (AD brugere)
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtService(IConfiguration configuration)
        {
            // Læser appsettings
            _secretKey = configuration["Jwt:SecretKey"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? "SuperSecretKey12345678910111213141516asdfghjkl!?JakobJensen";

            _issuer = configuration["Jwt:Issuer"]
                ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? "MyHotelApi";

            _audience = configuration["Jwt:Audience"]
                ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? "MyHotelFrontend";

            // Sat til 60 min lige nu. (Kunne Måske sættes til 2 for at demonstere at den udløber i login til eksamen?)
            _expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"]
                ?? Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES")
                ?? "1");
        }

        // Token til Db bruger bestående af id navn email og rolle - Måske Demonstrer til eksamen hvor man finder den og hvordan den ser ud som streng og oversat
        public string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var email = user.Email ?? string.Empty;
            var username = string.IsNullOrWhiteSpace(user.Username) ? email : user.Username;

            // Brug navigation hvis den er indlæst ellers fald tilbage til RoleId
            var roleName = user.Role?.Name ?? user.RoleId switch
            {
                1 => "Admin",
                2 => "Manager",
                3 => "Customer",
                4 => "Cleaner",
                _ => "Customer"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),       
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("email", email),
                new Claim(ClaimTypes.Name, username),
                new Claim("username", username),

                new Claim(ClaimTypes.Role, roleName),

                new Claim("role_id", user.RoleId.ToString())
            };

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

        // Token når vi kun kender brugernavn og rolle - til personale over AD ikke DB
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

        // Token med valgfri User og roller - det kan bruges både til DB bruger og personale
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

        // Tilføjer rolle claims kun ClaimTypes.Role for at undgå dubletter
        private void AddRoleClaims(List<Claim> claims, IEnumerable<string> roles)
        {
            foreach (var r in roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(r)) continue;
                claims.Add(new Claim(ClaimTypes.Role, r));
            }
        }
        
        // Bygger og signerer JWT
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

            return new JwtSecurityTokenHandler().WriteToken(token); //123
        }
    }
}

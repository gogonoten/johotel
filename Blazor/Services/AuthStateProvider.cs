using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;               

namespace Blazor.Services
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly TokenStorage _storage;

        private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

        public AuthStateProvider(TokenStorage storage) => _storage = storage;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _storage.GetTokenAsync();

            var principal = BuildPrincipalFromToken(token);

            return new AuthenticationState(principal);
        }

        public async Task MarkUserAsAuthenticatedAsync(string token)
        {
            await _storage.SetTokenAsync(token);

            var principal = BuildPrincipalFromToken(token);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }

        public async Task MarkUserAsLoggedOutAsync()
        {
            await _storage.ClearAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        public Task NotifyUserAuthentication(string token) => MarkUserAsAuthenticatedAsync(token);
        public Task NotifyUserLogout() => MarkUserAsLoggedOutAsync();

     
        private static ClaimsIdentity ValidateAndBuildIdentityFromToken(string? jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return new ClaimsIdentity();
            try 
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3) return new ClaimsIdentity();
                string payload = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out long exp))
                {
                    var expUtc = DateTimeOffset.FromUnixTimeSeconds(exp);
                    if (DateTimeOffset.UtcNow >= expUtc) return new ClaimsIdentity(); 
                }

                var claims = root.EnumerateObject()
                    .Where(p => p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                    .Select(p => new Claim(p.Name, p.Value.ToString()));
                return new ClaimsIdentity(claims, "jwt");
            }
            catch
            {
                return new ClaimsIdentity();
            }
        }

        private static ClaimsPrincipal BuildPrincipalFromToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return Anonymous;

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwt;

            try { jwt = handler.ReadJwtToken(token); }
            catch { return Anonymous; }

            var claims = new List<Claim>(jwt.Claims);

            bool hasStandardRole = claims.Any(c => c.Type == ClaimTypes.Role);
            if (!hasStandardRole)
            {
                foreach (var c in jwt.Claims)
                {
                    if (c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                        c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, c.Value));
                    }
                }
            }

            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                var name = jwt.Claims.FirstOrDefault(c =>
                               c.Type == "username" ||
                               c.Type == ClaimTypes.Name ||
                               c.Type == "unique_name" ||
                               c.Type == "name")
                           ?.Value ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                    claims.Add(new Claim(ClaimTypes.Name, name));
            }

            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "jwt",
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role
            );

            return new ClaimsPrincipal(identity);
        }
    }
}

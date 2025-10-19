using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Timers;
using Microsoft.JSInterop; // for toast messages
using Microsoft.AspNetCore.Components;
using Blazor.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly TokenStorage _storage;
    private readonly IJSRuntime _js;
    private readonly NavigationManager _nav;
    private System.Timers.Timer? _expiryTimer;

    public AuthStateProvider(TokenStorage storage, IJSRuntime js, NavigationManager nav)
    {
        _storage = storage;
        _js = js;
        _nav = nav;
    }

    // 🔹 Called automatically when the app starts or refreshes
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetTokenAsync();
        var principal = BuildPrincipalFromToken(token);

        if (!string.IsNullOrWhiteSpace(token))
            ScheduleAutoLogout(token);

        return new AuthenticationState(principal);
    }

    // 🔹 Called when user logs in or updates profile (new token)
    public async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await _storage.SetTokenAsync(token);
        var principal = BuildPrincipalFromToken(token);
        ScheduleAutoLogout(token);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    // 🔹 Called when user logs out or token expires
    public async Task MarkUserAsLoggedOutAsync(bool fromExpiry = false)
    {
        _expiryTimer?.Stop();
        _expiryTimer?.Dispose();
        _expiryTimer = null;

        await _storage.ClearAsync();

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));

        // Optional feedback when session expires
        if (fromExpiry)
        {
            await _js.InvokeVoidAsync("toast", "Din session er udløbet. Log ind igen.");
            _nav.NavigateTo("/login", forceLoad: true);
        }
    }

    // 🔹 Build a ClaimsPrincipal from JWT
    private ClaimsPrincipal BuildPrincipalFromToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new ClaimsPrincipal(new ClaimsIdentity());

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Check expiry immediately
            var exp = GetTokenExpiry(token);
            if (exp != null && exp.Value <= DateTimeOffset.UtcNow)
                return new ClaimsPrincipal(new ClaimsIdentity());

            var identity = new ClaimsIdentity(jwt.Claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    // 🔹 Reads "exp" claim from the JWT
    private static DateTimeOffset? GetTokenExpiry(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (expClaim == null)
                return null;

            if (long.TryParse(expClaim, out var expSeconds))
                return DateTimeOffset.FromUnixTimeSeconds(expSeconds);

            return null;
        }
        catch
        {
            return null;
        }
    }

    // 🔹 Schedule auto logout when JWT expires
    private void ScheduleAutoLogout(string token)
    {
        _expiryTimer?.Stop();
        _expiryTimer?.Dispose();
        _expiryTimer = null;

        var expiry = GetTokenExpiry(token);
        if (expiry == null)
            return;

        var msUntilExpiry = (expiry.Value - DateTimeOffset.UtcNow).TotalMilliseconds;
        if (msUntilExpiry <= 0)
        {
            _ = MarkUserAsLoggedOutAsync(true);
            return;
        }

        _expiryTimer = new System.Timers.Timer(msUntilExpiry);
        _expiryTimer.Elapsed += async (_, __) =>
        {
            _expiryTimer?.Stop();
            _expiryTimer?.Dispose();
            _expiryTimer = null;

            await MarkUserAsLoggedOutAsync(true);
        };
        _expiryTimer.AutoReset = false;
        _expiryTimer.Start();
    }
}

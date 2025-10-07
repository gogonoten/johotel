using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DomainModels;

namespace Blazor.Services
{
    /// <summary>
    /// HTTP-klient med auth-hjælpere (JWT i header + token storage).
    /// Understøtter unified login (AD + DB), logout og almindelige API-kald.
    /// </summary>
    public class APIService
    {
        private readonly HttpClient _http;
        private readonly TokenStorage _storage;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public APIService(HttpClient http, TokenStorage storage)
        {
            _http = http;
            _storage = storage;
        }

        public void SetBearer(string? token)
        {
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task ApplyAuthAsync()
        {
            var token = await _storage.GetTokenAsync();
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        }


        /// <summary>Gem token + sætter Authorisation header </summary>
        private async Task SetTokenPersistAndHeaderAsync(string token)
        {
            await _storage.SetTokenAsync(token);
            SetBearer(token);
        }

        /// <summary>Log ud - fjern token fra storage og ryd Authorisation header</summary>
        public async Task LogoutAsync()
        {
            await _storage.ClearAsync();
            SetBearer(null);
        }

        /// <summary>
        /// Hjælpemetode: returnerer korrekt route uanset om BaseAddress allerede ender på /api/
        /// </summary>
        private string ApiRoute(string relative)
        {
            var basePath = _http.BaseAddress?.AbsolutePath?.TrimEnd('/') ?? "";
            if (basePath.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                return relative.TrimStart('/');
            return $"api/{relative.TrimStart('/')}";
        }


        /// <summary>
        /// Unified login: prøver AD først for personale (username/UPN),
        /// falder tilbage til kundedatabasen (neon db) for e-mail/username.
        /// Returnerer JWT token eller null ved fejl.
        /// </summary>
        public async Task<string?> UnifiedLoginAsync(string usernameOrEmail, string password)
        {
            var body = new { UsernameOrEmail = usernameOrEmail, Password = password };
            var res = await _http.PostAsJsonAsync(ApiRoute("login"), body, _json);

            if (!res.IsSuccessStatusCode) return null;

            var dto = await res.Content.ReadFromJsonAsync<StaffLoginDto>(_json);
            if (dto is null || string.IsNullOrWhiteSpace(dto.token))
                return null;

            await SetTokenPersistAndHeaderAsync(dto.token);
            return dto.token;
        }

        /// <summary>
        /// Kun personale-endpoint (AD). 
        /// Returnerer ok, token, roller, fejl
        /// </summary>
        public async Task<(bool ok, string token, string[] roles, string? err)> StaffLoginAsync(string username, string password)
        {
            var resp = await _http.PostAsJsonAsync(ApiRoute("staff-auth/login"), new { username, password }, _json);
            if (!resp.IsSuccessStatusCode)
                return (false, "", Array.Empty<string>(), await resp.Content.ReadAsStringAsync());

            var dto = await resp.Content.ReadFromJsonAsync<StaffLoginDto>(_json);
            if (dto is null || string.IsNullOrWhiteSpace(dto.token))
                return (false, "", Array.Empty<string>(), "Tomt svar eller intet token");

            await SetTokenPersistAndHeaderAsync(dto.token);
            return (true, dto.token, dto.roles ?? Array.Empty<string>(), null);
        }

        // DTO til personale til Ad Og Db bestående af token, user, roles og source
        private sealed record StaffLoginDto(string token, string user, string[] roles, string? source);

        public async Task<T?> GetAsync<T>(string url)
        {
            await ApplyAuthAsync();
            return await _http.GetFromJsonAsync<T>(url, _json);
        }

        public async Task<HttpResponseMessage> GetRawAsync(string url)
        {
            await ApplyAuthAsync();
            return await _http.GetAsync(url);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
        {
            await ApplyAuthAsync();
            var res = await _http.PostAsJsonAsync(url, body, _json);
            if (!res.IsSuccessStatusCode) return default;
            return await res.Content.ReadFromJsonAsync<TResponse>(_json);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, object? body = null)
        {
            await ApplyAuthAsync();
            return body is null
                ? await _http.PostAsync(url, new StringContent("", Encoding.UTF8, "application/json"))
                : await _http.PostAsJsonAsync(url, body, _json);
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body)
        {
            await ApplyAuthAsync();
            var res = await _http.PutAsJsonAsync(url, body, _json);
            if (!res.IsSuccessStatusCode) return default;
            return await res.Content.ReadFromJsonAsync<TResponse>(_json);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            await ApplyAuthAsync();
            return await _http.DeleteAsync(url);
        }

        public record LoginRequest(string Username, string Password);
        public record RegisterRequest(string Username, string Email, string Password);

        public Task<HttpResponseMessage> LoginAsync(LoginDto dto) =>
            _http.PostAsJsonAsync("auth/login", dto);

        public Task<HttpResponseMessage> RegisterAsync(object anonymousDto) =>
            _http.PostAsJsonAsync("auth/register", anonymousDto);

        public async Task<string?> LoginAsync(string username, string password)
        {
            var res = await PostAsync("auth/login", new LoginRequest(username, password));
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("token", out var t))
                    return t.GetString();
            }
            catch { }
            return null;
        }

        public Task<HttpResponseMessage> RegisterAsync(string username, string email, string password) =>
            PostAsync("auth/register", new RegisterRequest(username, email, password));


        /// <summary>
        /// Bagudkompatibelt login mod ældre UsersController route
        /// Returnerer token eller nuller ved fejl
        /// </summary>
        public async Task<string?> LegacyLoginGetTokenAsync(string username, string password)
        {
            var res = await PostAsync(ApiRoute("users/login"), new LoginRequest(username, password));
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("token", out var t))
                    return t.GetString();
            }
            catch { }
            return null;
        }

//Rooms
        public Task<List<RoomDto>?> GetRoomsAsync() =>
            _http.GetFromJsonAsync<List<RoomDto>>("rooms", _json);

        public Task<List<RoomDto>?> GetRoomsAsync(DateTimeOffset from, DateTimeOffset to) =>
            _http.GetFromJsonAsync<List<RoomDto>>(
                $"rooms?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}", _json);

        public async Task<List<RoomDto>> GetRoomsOrEmptyAsync()
        {
            await ApplyAuthAsync();
            var list = await _http.GetFromJsonAsync<List<RoomDto>>("rooms", _json);
            return list ?? new List<RoomDto>();
        }

        public Task<RoomDto?> GetRoomAsync(int id) =>
            _http.GetFromJsonAsync<RoomDto>($"rooms/{id}", _json);

        public class SpanVm { public DateTimeOffset CheckIn { get; set; } public DateTimeOffset CheckOut { get; set; } }
        public Task<List<SpanVm>?> GetRoomBookedSpansAsync(int id, DateTimeOffset from, DateTimeOffset to) =>
            _http.GetFromJsonAsync<List<SpanVm>>(
                $"rooms/{id}/booked?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}",
                _json);

        //Bookings
        public Task<HttpResponseMessage> CreateBookingAsync(BookingDto dto) =>
            _http.PostAsJsonAsync("bookings", dto, _json);

        public Task<HttpResponseMessage> CancelBookingAsync(int id) =>
            _http.DeleteAsync($"bookings/{id}");

        public Task<HttpResponseMessage> GetMyBookingsAsync() =>
            _http.GetAsync("bookings/my");

        //Users
        public async Task<HttpResponseMessage> GetMeAsync()
        {
            await ApplyAuthAsync();
            return await _http.GetAsync("users/me");
        }

        public async Task<HttpResponseMessage> UpdateProfileAsync(UpdateProfileDto dto)
        {
            await ApplyAuthAsync();
            return await _http.PutAsJsonAsync("users/me", dto);
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordDto dto)
        {
            await ApplyAuthAsync();
            return await _http.PostAsJsonAsync("users/change-password", dto);
        }
    }
}

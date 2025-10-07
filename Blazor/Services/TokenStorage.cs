using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Blazor.Services
{
    // Enkel wrapper om localStorage til JWT
    public class TokenStorage
    {
        private readonly IJSRuntime _js;
        private const string TokenKey = "authToken";
        public TokenStorage(IJSRuntime js) => _js = js;

        // Gemmer token, hvis tom string = null
        public ValueTask SetTokenAsync(string? token) =>
            _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token ?? "");

        // Henter token. Null = tom/ingen er sat
        public async ValueTask<string?> GetTokenAsync()
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        public ValueTask ClearAsync() =>
            _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }
}

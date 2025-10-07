---
name: Uge 4 - Loginsystem med API & Blazor
about: Uge 4 - Loginsystem op med API med Blazor
title: Uge 4 - Loginsystem med API & Blazor
labels: ''
assignees: ''

---

## Loginsystem op med API med Blazor

- [ ] Opret login/register sider i Blazor
- [ ] Implementér authentication service i frontend
  - [ ] Håndter JWT tokens
  - [ ] Gem tokens i localStorage eller sessionStorage
- [ ] Opret protected routes/komponenter
- [ ] Implementér logout funktionalitet
- [ ] Test at login/logout virker med jeres API

---

### Eksempel på authentication service

```csharp
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public async Task<bool> LoginAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login",
            new { Username = username, Password = password });

        if (response.IsSuccessStatusCode)
        {
            var token = await response.Content.ReadAsStringAsync();
            await _localStorage.SetItemAsync("authToken", token);
            return true;
        }
        return false;
    }
}
```

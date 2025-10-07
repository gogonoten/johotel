---
name: Uge 4 - Opstart af Blazor projekt
about: Uge 4 - Opstart af Blazor projekt med fokus på API kald
title: Uge 4 - Opstart af Blazor projekt
labels: ''
assignees: ''

---

## Opstart af Blazor projekt med fokus på API kald

- [ ] Opret et nyt Blazor projekt (hvis ikke allerede gjort)
- [ ] Konfigurer HTTP client til at kalde jeres API
- [ ] Opret service-klasser til API kald
  - [ ] Implementér metoder til at hente data fra API
  - [ ] Håndter fejl og loading states
- [ ] Lav en simpel side der viser data fra jeres API
- [ ] Test at frontend kan kommunikere med backend

---

### Eksempel på API service

```csharp
public class UserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<User>>("api/users");
    }
}
```

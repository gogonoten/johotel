---
name: Uge 3 - XML Dokumentation af API
about: Uge 3 - XML Dokumentation af API
title: Uge 3 - XML Dokumentation af API
labels: ''
assignees: ''

---

## XML Dokumentation af API

- [ ] Tilføj XML-kommentarer til alle controllere og endpoints
  - [ ] Brug <summary>, <param>, <returns> og mindst én <response code="..."> pr. endpoint
- [ ] Aktivér XML-dokumentation i jeres .csproj-fil
- [ ] Konfigurer Swagger til at vise XML-dokumentation
- [ ] Bekræft at dokumentationen vises i Swagger UI

---

### Eksempel på XML-kommentar

```csharp
/// <summary>
/// Henter en specifik bruger ud fra ID.
/// </summary>
/// <param name="id">Brugerens unikke ID.</param>
/// <returns>Brugerens detaljer.</returns>
/// <response code="404">Bruger ikke fundet.</response>
[HttpGet("{id}")]
public async Task<ActionResult<User>> GetUser(string id)
{
    ...
}
```

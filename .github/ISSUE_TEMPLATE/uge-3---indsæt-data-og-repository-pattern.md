---
name: Uge 3 - Indsæt data & Repository Pattern
about: Uge 3 - Indsæt data & Repository Pattern
title: Uge 3 - Indsæt data & Repository Pattern
labels: ''
assignees: ''

---

## Indsæt data samt repository pattern og struktur

- [ ] Indsæt eksempeldata i databasen (fx brugere, værelser, bookinger)
  - [ ] Brug AI eller Faker (Bogus)
- [ ] Implementér repository pattern for mindst én model
  - [ ] Opret interface og konkret repository-klasse
  - [ ] Brug repository i din controller
- [ ] Refaktorer kode så ansvar er adskilt (fx services, repositories, controllers)

---

### Eksempel på repository interface

```csharp
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);
    Task AddAsync(User user);
    // ... flere metoder efter behov
}
```

### Eksempel på seed-metode

```csharp
public static void SeedData(this ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>().HasData(
        new User { Id = "1", Name = "Alice" },
        new User { Id = "2", Name = "Bob" }
    );
}
```

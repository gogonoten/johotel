# Filoversigt

Dette dokument giver en oversigt over fil- og mappestrukturen i H2-projektet.

## Projektstruktur

```
.
├── API
│   ├── Controllers
│   │   ├── StatusController.cs
│   │   └── WeatherForecastController.cs
│   ├── Properties
│   │   └── launchSettings.json
│   ├── API.csproj
│   ├── API.http
│   ├── appsettings.example.json
│   ├── Dockerfile
│   └── Program.cs
├── Blazor
│   ├── Components
│   │   ├── Counter.razor
│   │   ├── README.md
│   │   ├── StatusCard.razor
│   │   └── WeatherTable.razor
│   ├── Layout
│   │   ├── MainLayout.razor
│   │   ├── MainLayout.razor.css
│   │   ├── NavMenu.razor
│   │   └── NavMenu.razor.css
│   ├── Pages
│   │   └── Home.razor
│   ├── Properties
│   │   └── launchSettings.json
│   ├── Services
│   │   ├── APIService.cs
│   │   ├── APIService.Status.cs
│   │   └── APIService.Weather.cs
│   ├── wwwroot
│   │   ├── css
│   │   ├── sample-data
│   │   └── lib
│   ├── _Imports.razor
│   ├── App.razor
│   ├── Blazor.csproj
│   └── Program.cs
├── DomainModels
│   ├── HealthCheckResponse.cs
│   └── WeatherForecast.cs
├── H2-Projekt.AppHost
│   ├── Properties
│   │   └── launchSettings.json
│   ├── H2-Projekt.AppHost.csproj
│   └── Program.cs
├── H2-Projekt.ServiceDefaults
│   ├── Extensions.cs
│   └── H2-Projekt.ServiceDefaults.csproj
├── .gitignore
├── H2-Projekt.sln
└── README.md
```

## Root `/`

- `.gitignore`: Angiver filer og mapper, som Git skal ignorere.
- `H2-Projekt.sln`: Løsningsfil til Visual Studio, der samler alle projekterne.
- `README.md`: Generel information om projektet.
- `files.md`: Denne fil.

## `API/`

Dette er et ASP.NET Core Web API-projekt, der fungerer som backend.

- `API.csproj`: Projektfilen, der definerer afhængigheder og byggeindstillinger for API'en.
- `API.http`: Fil til at teste API-endepunkter direkte i Visual Studio.
- `appsettings.example.json`: Eksempel på konfigurationsfil.
- `Dockerfile`: Til at bygge en Docker-container for API'en.
- `Program.cs`: Applikationens startpunkt, hvor services og middleware konfigureres.
- `Controllers/`: Indeholder API-controllere.
  - `StatusController.cs`: Håndterer anmodninger om applikationens status.
  - `WeatherForecastController.cs`: Håndterer anmodninger om vejrprognoser.
- `Properties/`:
  - `launchSettings.json`: Konfiguration for at starte projektet i udviklingsmiljøet.

## `Blazor/`

Dette er Blazor Web App-projektet, der udgør frontend.

- `_Imports.razor`: Globale using-direktiver for Razor-komponenter.
- `App.razor`: Rodkomponenten i Blazor-applikationen.
- `Blazor.csproj`: Projektfilen for Blazor-appen.
- `Components/`: Genbrugelige Blazor-komponenter.
  - `Counter.razor`: En simpel tællerkomponent.
  - `README.md`: Dokumentation for komponenterne.
  - `StatusCard.razor`: Komponent til at vise status fra API'en.
  - `WeatherTable.razor`: Komponent til at vise vejrdata i en tabel.
- `Layout/`: Definerer applikationens layout.
  - `MainLayout.razor`: Hovedlayout for siderne.
  - `NavMenu.razor`: Navigationsmenuen.
- `Pages/`: Routable sider i applikationen.
  - `Home.razor`: Forsiden.
- `Program.cs`: Applikationens startpunkt.
- `Services/`: Services til brug i Blazor-appen.
  - `APIService.cs`: Base-service til kommunikation med API'en.
  - `APIService.Status.cs`: Partiel klasse til status-relaterede API-kald.
  - `APIService.Weather.cs`: Partiel klasse til vejr-relaterede API-kald.
- `wwwroot/`: Statiske filer som CSS, JavaScript og billeder.
  - `sample-data/weather.json`: Eksempeldata til udvikling.

## `DomainModels/`

Et klassebibliotek med datamodeller, der deles mellem `API` og `Blazor` projekterne.

- `DomainModels.csproj`: Projektfilen for klassebiblioteket.
- `HealthCheckResponse.cs`: Model for svar på health check.
- `WeatherForecast.cs`: Model for vejrprognosedata.

## `H2-Projekt.AppHost/`

Et .NET Aspire AppHost-projekt, der orkestrerer de forskellige services i applikationen.

- `H2-Projekt.AppHost.csproj`: Projektfilen.
- `Program.cs`: Definerer hvilke projekter der skal startes og hvordan de er forbundet.

## `H2-Projekt.ServiceDefaults/`

Et .NET Aspire Service Defaults-projekt, der indeholder standardkonfiguration for services.

- `H2-Projekt.ServiceDefaults.csproj`: Projektfilen.
- `Extensions.cs`: Udvidelsesmetoder til at tilføje standardkonfigurationer.

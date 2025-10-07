Formålet med Services i et Blazor-projekt
I et Blazor-projekt bruges mappen Services til at organisere og opbevare serviceklasser, der håndterer forretningslogik, dataadgang og kommunikation med eksterne API'er. Services adskiller logik fra UI-komponenter, hvilket gør koden mere genanvendelig, testbar og overskuelig. Typiske eksempler på services er klasser, der henter data fra en database eller et web-API, eller som indeholder logik, der skal bruges flere steder i applikationen.
Eksempler på services:
APIService: Håndterer kald til backend-API'er.
WeatherService: Henter og behandler vejrdata.
Services registreres ofte som afhængigheder (dependency injection) i Blazor, så de nemt kan bruges i komponenter.

Forskellen på Services og Helpers
Services bør bruges til:

- Forretningslogik, der involverer datahentning, databehandling eller kommunikation med eksterne systemer (f.eks. API-kald).
- Funktionalitet, der skal kunne genbruges på tværs af flere komponenter, og som ofte kræver afhængigheder eller konfiguration.
- Logik, der kan have brug for at blive testet isoleret fra UI.

Helpers bør bruges til:

- Små hjælpefunktioner, der ikke har afhængigheder eller kræver konfiguration.
- Utility-metoder til f.eks. formatering, konvertering eller simple beregninger.
- Funktioner, der ikke har behov for at blive injiceret som en service, men blot kan kaldes statisk eller direkte.

Kort sagt: Services håndterer større logik og integration, mens helpers understøtter med små, simple funktioner.

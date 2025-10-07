# Docker Compose Setup for H2 Projekt

Denne guide forklarer, hvordan du kører H2 projektet med Docker Compose.

## Forudsætninger

- Docker Desktop installeret og kørende
- Docker Compose v2 (kommer med Docker Desktop)

## Hurtig start

1. **Kør hele stacket:**
   ```bash
   docker-compose up -d
   ```

2. **Se logs:**
   ```bash
   docker-compose logs -f
   ```

3. **Stop alle services:**
   ```bash
   docker-compose down
   ```

## Services

### API Service
- **Port:** 8050 (HTTP), 8051 (HTTPS)
- **URL:** http://localhost:8050
- **Health Check:** http://localhost:8050/health
- **Swagger UI:** http://localhost:8050/swagger

### Blazor WASM App
- **Port:** 8052
- **URL:** http://localhost:8052
- **Health Check:** http://localhost:8052
- **Automatisk API Detection:** Appen finder automatisk den rigtige API endpoint

## Automatisk API Endpoint Detection

Blazor appen har nu en intelligent endpoint detection der automatisk finder den rigtige API:

### Fallback Rækkefølge:
1. **Docker Compose:** `http://api:8080/` (internt i Docker netværk)
2. **Local Development:** `http://localhost:5253/` (Visual Studio)
3. **Production:** `https://h2-api.mercantec.tech/` (produktions server)

### Hvordan det virker:
- Appen tester hver endpoint med en health check
- Vælger den første der responderer
- Logger hvilken endpoint der bruges
- Har en sikker fallback hvis ingen virker

## Docker Compose Kommandoer

### Opbygning og kørsel
```bash
# Byg og start alle services
docker-compose up --build

# Kør i baggrunden
docker-compose up -d --build

# Kun byg images uden at køre
docker-compose build
```

### Overvågning
```bash
# Se status på alle services
docker-compose ps

# Se logs fra alle services
docker-compose logs

# Se logs fra specifik service
docker-compose logs api
docker-compose logs blazor

# Følg logs i realtid
docker-compose logs -f
```

### Vedligeholdelse
```bash
# Stop alle services
docker-compose down

# Stop og fjern volumes
docker-compose down -v

# Genstart en specifik service
docker-compose restart api

# Fjern alle images og containers
docker-compose down --rmi all
```

## Konfiguration

### Miljøvariabler
Du kan tilpasse miljøvariabler ved at oprette en `.env` fil:

```env
# API miljøvariabler
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080;https://+:8081

# Database connection string (hvis nødvendigt)
# ConnectionStrings__DefaultConnection=your_connection_string
```

### Ports
Hvis du vil ændre ports, kan du modificere `compose.yaml`:

```yaml
services:
  api:
    ports:
      - "8050:8080"  # Ændre fra 8050 til 5000
      - "8051:8081"  # Ændre fra 8051 til 5001
  
  blazor:
    ports:
      - "8052:80"    # Ændre fra 8052 til 3000
```

## Fejlfinding

### Tjek service status
```bash
docker-compose ps
```

### Se detaljerede logs
```bash
docker-compose logs api
docker-compose logs blazor
```

### Tjek health checks
```bash
# API health check
curl http://localhost:8050/health

# Blazor health check
curl http://localhost:8052
```

### Genstart services
```bash
# Genstart specifik service
docker-compose restart api

# Genstart alle services
docker-compose restart
```

## Udvikling

### Hot reload (udvikling)
For udvikling med hot reload, kan du køre services separat:

```bash
# Kør kun API
docker-compose up api

# Kør kun Blazor
docker-compose up blazor
```

### Debugging
For at debugge i Visual Studio:
1. Kør `docker-compose up api` for at starte API'en
2. Kør Blazor appen lokalt i Visual Studio
3. Konfigurer Blazor til at kalde API'en på `http://localhost:8080`

## Produktion

For produktion, anbefales det at:
1. Ændre `ASPNETCORE_ENVIRONMENT` til `Production`
2. Konfigurere HTTPS korrekt
3. Tilføje reverse proxy (f.eks. nginx) foran services
4. Konfigurere logging og monitoring
5. Tilføje database service hvis nødvendigt 
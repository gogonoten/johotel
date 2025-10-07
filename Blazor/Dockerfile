# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Build arguments for API endpoint
ARG API_ENDPOINT=http://localhost:8051/

# Copy project files
COPY ["Blazor/Blazor.csproj", "Blazor/"]
COPY ["DomainModels/DomainModels.csproj", "DomainModels/"]
COPY ["API/API.csproj", "API/"]
COPY ["H2-Projekt.ServiceDefaults/H2-Projekt.ServiceDefaults.csproj", "H2-Projekt.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "Blazor/Blazor.csproj"

# Copy source code
COPY . .

# Build Blazor app
WORKDIR "/src/Blazor"
RUN dotnet build "Blazor.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Blazor.csproj" -c Release -o /app/publish

# Runtime stage - use nginx with simple configuration
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .

# Set API endpoint as environment variable for runtime
ENV API_ENDPOINT=$API_ENDPOINT

COPY Blazor/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80 
# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# --- Gmail Certificate
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates tzdata \
 && rm -rf /var/lib/apt/lists/* \
 && update-ca-certificates

ENV TZ=Europe/Copenhagen


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG BUILD_DATE=unknown
WORKDIR /src
COPY ["API/API.csproj", "API/"]
RUN dotnet restore "./API/API.csproj"
COPY . .
WORKDIR "/src/API"
RUN dotnet build "./API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
ARG BUILD_DATE=unknown
LABEL org.label-schema.build-date=$BUILD_DATE

WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables for container
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Switch to non-root user
USER $APP_UID

ENTRYPOINT ["dotnet", "API.dll"]
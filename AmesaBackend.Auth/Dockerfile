# Use the official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

# Use the official .NET 8.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["AmesaBackend.Auth/AmesaBackend.Auth.csproj", "AmesaBackend.Auth/"]
COPY ["AmesaBackend.Shared/AmesaBackend.Shared.csproj", "AmesaBackend.Shared/"]
RUN dotnet restore "AmesaBackend.Auth/AmesaBackend.Auth.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "AmesaBackend.Auth/AmesaBackend.Auth.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "AmesaBackend.Auth/AmesaBackend.Auth.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Install curl for health checks
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=publish /app/publish .

# Create directories for logs
RUN mkdir -p /app/logs
RUN chmod 755 /app/logs

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "AmesaBackend.Auth.dll"]


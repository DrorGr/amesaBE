FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AmesaBackend.Lottery/AmesaBackend.Lottery.csproj", "AmesaBackend.Lottery/"]
COPY ["AmesaBackend.Shared/AmesaBackend.Shared.csproj", "AmesaBackend.Shared/"]
RUN dotnet restore "AmesaBackend.Lottery/AmesaBackend.Lottery.csproj"
COPY . .
RUN dotnet build "AmesaBackend.Lottery/AmesaBackend.Lottery.csproj" -c Release -o /app/build

FROM build AS publish
# Temporarily rename appsettings files from referenced projects to prevent conflicts during publish
RUN find /src -name "appsettings*.json" -not -path "*/AmesaBackend.Lottery/*" -exec mv {} {}.bak \; 2>/dev/null || true
RUN dotnet publish "AmesaBackend.Lottery/AmesaBackend.Lottery.csproj" -c Release -o /app/publish /p:UseAppHost=false
# Restore appsettings files
RUN find /src -name "appsettings*.json.bak" -exec sh -c 'mv "$1" "${1%.bak}"' _ {} \; 2>/dev/null || true

FROM base AS final
WORKDIR /app
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
RUN mkdir -p /app/logs && chmod 755 /app/logs
RUN groupadd -r appuser && useradd -r -g appuser appuser && chown -R appuser:appuser /app
USER appuser
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 CMD curl -f http://localhost:8080/health || exit 1
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AmesaBackend.Lottery.dll"]


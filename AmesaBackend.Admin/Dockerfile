FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AmesaBackend.Admin/AmesaBackend.Admin.csproj", "AmesaBackend.Admin/"]
COPY ["AmesaBackend.Shared/AmesaBackend.Shared.csproj", "AmesaBackend.Shared/"]
RUN dotnet restore "AmesaBackend.Admin/AmesaBackend.Admin.csproj"
COPY ["AmesaBackend.Admin/", "AmesaBackend.Admin/"]
COPY ["AmesaBackend.Shared/", "AmesaBackend.Shared/"]
RUN dotnet build "AmesaBackend.Admin/AmesaBackend.Admin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AmesaBackend.Admin/AmesaBackend.Admin.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
ENTRYPOINT ["dotnet", "AmesaBackend.Admin.dll"]


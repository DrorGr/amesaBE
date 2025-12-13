FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy project files for all referenced projects
COPY ["AmesaBackend.Admin/AmesaBackend.Admin.csproj", "AmesaBackend.Admin/"]
COPY ["AmesaBackend.Shared/AmesaBackend.Shared.csproj", "AmesaBackend.Shared/"]
COPY ["AmesaBackend/AmesaBackend.csproj", "AmesaBackend/"]
COPY ["AmesaBackend.Auth/AmesaBackend.Auth.csproj", "AmesaBackend.Auth/"]
COPY ["AmesaBackend.Lottery/AmesaBackend.Lottery.csproj", "AmesaBackend.Lottery/"]
COPY ["AmesaBackend.Payment/AmesaBackend.Payment.csproj", "AmesaBackend.Payment/"]
COPY ["AmesaBackend.Content/AmesaBackend.Content.csproj", "AmesaBackend.Content/"]
COPY ["AmesaBackend.LotteryResults/AmesaBackend.LotteryResults.csproj", "AmesaBackend.LotteryResults/"]
COPY ["AmesaBackend.Notification/AmesaBackend.Notification.csproj", "AmesaBackend.Notification/"]
RUN dotnet restore "AmesaBackend.Admin/AmesaBackend.Admin.csproj"
# Copy source code for all referenced projects
COPY ["AmesaBackend.Admin/", "AmesaBackend.Admin/"]
COPY ["AmesaBackend.Shared/", "AmesaBackend.Shared/"]
COPY ["AmesaBackend/", "AmesaBackend/"]
COPY ["AmesaBackend.Auth/", "AmesaBackend.Auth/"]
COPY ["AmesaBackend.Lottery/", "AmesaBackend.Lottery/"]
COPY ["AmesaBackend.Payment/", "AmesaBackend.Payment/"]
COPY ["AmesaBackend.Content/", "AmesaBackend.Content/"]
COPY ["AmesaBackend.LotteryResults/", "AmesaBackend.LotteryResults/"]
COPY ["AmesaBackend.Notification/", "AmesaBackend.Notification/"]
# Remove appsettings.json files from referenced projects to avoid conflicts
RUN rm -f /src/AmesaBackend/appsettings.json /src/AmesaBackend/appsettings.*.json \
    /src/AmesaBackend.Auth/appsettings.json /src/AmesaBackend.Auth/appsettings.*.json \
    /src/AmesaBackend.Lottery/appsettings.json /src/AmesaBackend.Lottery/appsettings.*.json \
    /src/AmesaBackend.Payment/appsettings.json /src/AmesaBackend.Payment/appsettings.*.json \
    /src/AmesaBackend.Content/appsettings.json /src/AmesaBackend.Content/appsettings.*.json \
    /src/AmesaBackend.LotteryResults/appsettings.json /src/AmesaBackend.LotteryResults/appsettings.*.json \
    /src/AmesaBackend.Notification/appsettings.json /src/AmesaBackend.Notification/appsettings.*.json
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


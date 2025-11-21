@echo off
echo ===============================================
echo AmesaBackend Database Seeder
echo ===============================================
echo.

REM Check if .NET 8 is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET 8 SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK Version:
dotnet --version
echo.

REM Set environment
set ASPNETCORE_ENVIRONMENT=Development
if "%1"=="prod" set ASPNETCORE_ENVIRONMENT=Production
if "%1"=="production" set ASPNETCORE_ENVIRONMENT=Production

echo Environment: %ASPNETCORE_ENVIRONMENT%
echo.

REM Build and run
echo Building seeder...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Running database seeder...
echo.
dotnet run --configuration Release

echo.
echo Seeder execution completed.
pause

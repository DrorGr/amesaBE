# AmesaBackend Database Seeder

A standalone console application for seeding the AmesaBackend database with comprehensive demo data.

## 🎯 **Features**

- **Standalone Operation**: Runs independently of the main application
- **Environment-Aware**: Supports Development and Production configurations
- **Comprehensive Data**: Seeds languages, translations, users, houses, and related data
- **Safety Features**: Production confirmation prompts and detailed logging
- **Batch Processing**: Efficient data insertion with configurable batch sizes
- **Error Handling**: Robust error handling with detailed logging

## 🚀 **Quick Start**

### Prerequisites

- .NET 8.0 SDK
- Access to PostgreSQL database (local or production)

### Running the Seeder

#### Option 1: Using Batch File (Windows)
```bash
# Development environment (default)
run-seeder.bat

# Production environment
run-seeder.bat prod
```

#### Option 2: Using PowerShell (Cross-platform)
```powershell
# Development environment (default)
.\run-seeder.ps1

# Production environment
.\run-seeder.ps1 -Environment production
```

#### Option 3: Using .NET CLI
```bash
# Development
dotnet run

# Production
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run
```

## ⚙️ **Configuration**

### Connection Strings

Configure database connections in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=amesa_dev;Username=postgres;Password=password;Port=5432;",
    "ProductionConnection": "Host=amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_prod;Username=amesa_admin;Password=u1fwn3s9;Port=5432;"
  }
}
```

### Seeder Settings

Customize seeding behavior:

```json
{
  "SeederSettings": {
    "Environment": "Development",
    "TruncateExistingData": true,
    "SeedLanguages": true,
    "SeedTranslations": true,
    "SeedUsers": true,
    "SeedHouses": true,
    "BatchSize": 100,
    "EnableDetailedLogging": true
  }
}
```

## 📊 **Seeded Data Overview**

### Languages (6 entries)
- English (default)
- Hebrew
- Arabic  
- Spanish
- French
- Polish

### Translations (80+ entries)
- Navigation menu items
- Hero section content
- Authentication forms
- House listing labels
- Common UI elements
- Footer content

### Users (5 demo accounts)
- **Admin**: admin@amesa.com / Admin123!
- **John Doe**: john.doe@example.com / Password123!
- **Sarah Wilson**: sarah.wilson@example.com / Password123!
- **Ahmed Hassan**: ahmed.hassan@example.com / Password123!
- **Maria Garcia**: maria.garcia@example.com / Password123!

### Houses (8 luxury properties)
1. **Luxury Villa in Warsaw** - €1,200,000
2. **Modern Apartment in Kraków** - €800,000
3. **Historic House in Gdańsk** - €900,000
4. **Beachfront Condo in Sopot** - €600,000
5. **Mountain Villa in Zakopane** - €1,000,000
6. **Modern Penthouse in Wrocław** - €700,000
7. **Lake House in Mazury** - €500,000
8. **Historic Mansion in Poznań** - €1,100,000

Each house includes:
- Detailed descriptions
- Property specifications
- Features array
- Lottery information
- High-quality images

## 🔒 **Security Features**

### Production Safety
- **Confirmation Prompt**: Requires typing "YES" for production seeding
- **Connection String Masking**: Passwords hidden in logs
- **Environment Validation**: Clear environment identification

### Password Security
- **SHA256 + Salt**: Secure password hashing
- **Consistent Salt**: Uses application-specific salt value
- **Demo Credentials**: Clearly marked test accounts

## 📝 **Logging**

The seeder provides comprehensive logging:

```
=== AmesaBackend Database Seeder ===
Environment: Production
Connection: Host=amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_prod;Username=amesa_admin;Password=***;Port=5432
=====================================

Starting database seeding process...
Ensuring database exists and is accessible...
Database connection successful
Truncating existing data...
Existing data truncated successfully
Seeding languages...
Seeded 6 languages
Seeding translations...
Processed translation batch 1, items: 50
Processed translation batch 2, items: 30
Seeded 80 translations
Seeding users...
Seeded 5 users, 4 addresses, 4 phones
Seeding houses...
Seeded 4 houses and 4 house images

=== SEEDING RESULTS ===
Languages seeded: 6
Translations seeded: 80
Users seeded: 5
User addresses seeded: 4
User phones seeded: 4
Houses seeded: 4
House images seeded: 4
=== SEEDING COMPLETED SUCCESSFULLY ===
```

## 🛠️ **Development**

### Project Structure
```
AmesaBackend.DatabaseSeeder/
├── Models/
│   └── SeederSettings.cs          # Configuration model
├── Services/
│   ├── DatabaseSeederService.cs   # Main seeding logic
│   └── PasswordHashingService.cs  # Password utilities
├── Program.cs                     # Application entry point
├── appsettings.json              # Development config
├── appsettings.Production.json   # Production config
├── run-seeder.bat               # Windows batch script
├── run-seeder.ps1              # PowerShell script
└── README.md                   # This file
```

### Adding New Data

To add new seeding data:

1. **Extend DatabaseSeederService**: Add new seeding methods
2. **Update Configuration**: Add new settings if needed
3. **Maintain Order**: Respect foreign key constraints
4. **Test Thoroughly**: Verify in both environments

### Building

```bash
# Build
dotnet build

# Build for release
dotnet build --configuration Release

# Publish (optional)
dotnet publish --configuration Release --output ./publish
```

## 🚨 **Important Notes**

### Production Usage
- ⚠️ **DESTRUCTIVE OPERATION**: Truncates existing data
- ✅ **Confirmation Required**: Must type "YES" to proceed
- 🔒 **Secure Credentials**: Uses production database credentials
- 📊 **Demo Data Only**: Intended for development/testing

### Troubleshooting

**Connection Issues**:
- Verify database credentials
- Check network connectivity
- Ensure PostgreSQL is running

**Permission Errors**:
- Verify database user permissions
- Check schema access rights
- Ensure TRUNCATE permissions

**Build Errors**:
- Ensure .NET 8 SDK is installed
- Restore NuGet packages: `dotnet restore`
- Check project references

## 📞 **Support**

For issues or questions:
1. Check the logs for detailed error information
2. Verify database connectivity
3. Ensure all prerequisites are met
4. Review configuration settings

---

**Created**: 2024-11-21  
**Version**: 1.0.0  
**Status**: Production Ready ✅

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AmesaBackend.Data;
using AmesaBackend.Models;
using AmesaBackend.DatabaseSeeder.Models;
using AmesaBackend.DatabaseSeeder.Services;
using static AmesaBackend.Models.GenderType;
using static AmesaBackend.Models.UserStatus;
using static AmesaBackend.Models.UserVerificationStatus;
using static AmesaBackend.Models.AuthProvider;
using static AmesaBackend.Models.LotteryStatus;
using static AmesaBackend.Models.MediaType;

namespace AmesaBackend.DatabaseSeeder.Services
{
    public class DatabaseSeederService_Fixed
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<DatabaseSeederService_Fixed> _logger;
        private readonly SeederSettings _settings;

        public DatabaseSeederService_Fixed(
            AmesaDbContext context,
            ILogger<DatabaseSeederService_Fixed> logger,
            IOptions<SeederSettings> settings)
        {
            _context = context;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task SeedDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding process...");
                _logger.LogInformation($"Environment: {_settings.Environment}");
                _logger.LogInformation($"Truncate existing data: {_settings.TruncateExistingData}");

                // Ensure database exists and is accessible
                await EnsureDatabaseAsync();

                // SAFETY CHECK: Never truncate in production
                if (_settings.TruncateExistingData && _settings.Environment.ToLower() != "production")
                {
                    await TruncateExistingDataAsync();
                }
                else if (_settings.TruncateExistingData && _settings.Environment.ToLower() == "production")
                {
                    _logger.LogWarning("SAFETY: Truncation disabled for production environment!");
                }

                // Seed data in correct order (respecting foreign key constraints)
                if (_settings.SeedLanguages)
                {
                    await SeedLanguagesAsync();
                }

                if (_settings.SeedTranslations)
                {
                    await SeedTranslationsFromFilesAsync();
                }

                if (_settings.SeedUsers)
                {
                    await SeedUsersAsync();
                }

                if (_settings.SeedHouses)
                {
                    await SeedHousesAsync();
                }

                _logger.LogInformation("Database seeding completed successfully!");
                await LogSeedingResultsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");
                throw;
            }
        }

        private async Task EnsureDatabaseAsync()
        {
            _logger.LogInformation("Ensuring database exists and is accessible...");
            
            try
            {
                await _context.Database.OpenConnectionAsync();
                await _context.Database.CloseConnectionAsync();
                _logger.LogInformation("Database connection successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to database");
                throw;
            }
        }

        private async Task TruncateExistingDataAsync()
        {
            _logger.LogInformation("Truncating existing data...");

            try
            {
                // Truncate in correct order to respect foreign key constraints
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_lottery.\"HouseImages\" CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_lottery.\"Houses\" CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_auth.\"UserPhones\" CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_auth.\"UserAddresses\" CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_auth.\"Users\" CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_content.\"Translations\" CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_content.\"Languages\" CASCADE;");

                _logger.LogInformation("Existing data truncated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error truncating existing data");
                throw;
            }
        }

        private async Task SeedLanguagesAsync()
        {
            _logger.LogInformation("Seeding languages...");

            var existingLanguages = await _context.Languages.CountAsync();
            if (existingLanguages > 0)
            {
                _logger.LogInformation($"Languages already exist ({existingLanguages} found), skipping...");
                return;
            }

            var languages = new List<Language>
            {
                new Language { Code = "en", Name = "English", NativeName = "English", IsActive = true, IsDefault = true },
                new Language { Code = "he", Name = "Hebrew", NativeName = "עברית", IsActive = true, IsDefault = false },
                new Language { Code = "ar", Name = "Arabic", NativeName = "العربية", IsActive = true, IsDefault = false },
                new Language { Code = "es", Name = "Spanish", NativeName = "Español", IsActive = true, IsDefault = false },
                new Language { Code = "fr", Name = "French", NativeName = "Français", IsActive = true, IsDefault = false },
                new Language { Code = "pl", Name = "Polish", NativeName = "Polski", IsActive = true, IsDefault = false }
            };

            await _context.Languages.AddRangeAsync(languages);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Seeded {languages.Count} languages successfully");
        }

        private async Task SeedTranslationsFromFilesAsync()
        {
            _logger.LogInformation("Seeding translations from external SQL files...");

            var existingTranslations = await _context.Translations.CountAsync();
            if (existingTranslations > 0)
            {
                _logger.LogInformation($"Translations already exist ({existingTranslations} found), skipping...");
                return;
            }

            try
            {
                // First, seed the main comprehensive translations (5 languages: en, he, ar, es, fr)
                var comprehensiveTranslationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "comprehensive-translations.sql");
                if (File.Exists(comprehensiveTranslationsPath))
                {
                    _logger.LogInformation("Loading comprehensive translations from file...");
                    var comprehensiveSql = await File.ReadAllTextAsync(comprehensiveTranslationsPath);
                    
                    // Add ON CONFLICT clause to handle duplicates
                    if (!comprehensiveSql.Contains("ON CONFLICT"))
                    {
                        comprehensiveSql = comprehensiveSql.TrimEnd(';') + " ON CONFLICT (\"LanguageCode\", \"Key\") DO NOTHING;";
                    }
                    
                    await _context.Database.ExecuteSqlRawAsync(comprehensiveSql);
                    _logger.LogInformation("Comprehensive translations seeded successfully");
                }
                else
                {
                    _logger.LogWarning($"Comprehensive translations file not found at: {comprehensiveTranslationsPath}");
                }

                // Then, seed Polish translations addon
                var polishTranslationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "polish-translations-addon.sql");
                if (File.Exists(polishTranslationsPath))
                {
                    _logger.LogInformation("Loading Polish translations addon from file...");
                    var polishSql = await File.ReadAllTextAsync(polishTranslationsPath);
                    
                    // Add ON CONFLICT clause to handle duplicates
                    if (!polishSql.Contains("ON CONFLICT"))
                    {
                        polishSql = polishSql.TrimEnd(';') + " ON CONFLICT (\"LanguageCode\", \"Key\") DO NOTHING;";
                    }
                    
                    await _context.Database.ExecuteSqlRawAsync(polishSql);
                    _logger.LogInformation("Polish translations seeded successfully");
                }
                else
                {
                    _logger.LogWarning($"Polish translations file not found at: {polishTranslationsPath}");
                }

                // Log final count
                var finalCount = await _context.Translations.CountAsync();
                _logger.LogInformation($"Total translations after seeding: {finalCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding translations from files");
                throw;
            }
        }

        private async Task SeedUsersAsync()
        {
            _logger.LogInformation("Seeding users...");

            var existingUsers = await _context.Users.CountAsync();
            if (existingUsers > 0)
            {
                _logger.LogInformation($"Users already exist ({existingUsers} found), skipping...");
                return;
            }

            var passwordHashingService = new PasswordHashingService();
            var users = new List<User>();

            // Create test users
            for (int i = 1; i <= 5; i++)
            {
                var user = new User
                {
                    Email = $"user{i}@amesa.com",
                    FirstName = $"User{i}",
                    LastName = "Test",
                    PasswordHash = passwordHashingService.HashPassword("Password123!"),
                    Gender = i % 2 == 0 ? Male : Female,
                    DateOfBirth = DateTime.Now.AddYears(-25 - i),
                    Status = Active,
                    VerificationStatus = Verified,
                    AuthProvider = Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                };

                users.Add(user);
            }

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Seeded {users.Count} users successfully");
        }

        private async Task SeedHousesAsync()
        {
            _logger.LogInformation("Seeding houses with images...");

            var existingHouses = await _context.Houses.CountAsync();
            if (existingHouses > 0)
            {
                _logger.LogInformation($"Houses already exist ({existingHouses} found), skipping...");
                return;
            }

            var houses = new List<House>
            {
                new House
                {
                    Title = "Luxury Villa in Tel Aviv",
                    Description = "Beautiful modern villa with sea view",
                    Price = 2500000,
                    TicketPrice = 100,
                    TotalTickets = 25000,
                    TicketsSold = 0,
                    City = "Tel Aviv",
                    Address = "123 Rothschild Blvd",
                    Bedrooms = 5,
                    Bathrooms = 4,
                    SquareMeters = 350,
                    LotSize = 500,
                    YearBuilt = 2020,
                    PropertyType = "Villa",
                    Status = Active,
                    DrawDate = DateTime.Now.AddDays(30),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                },
                new House
                {
                    Title = "Penthouse in Jerusalem",
                    Description = "Stunning penthouse with panoramic city views",
                    Price = 3200000,
                    TicketPrice = 150,
                    TotalTickets = 21333,
                    TicketsSold = 0,
                    City = "Jerusalem",
                    Address = "456 King David St",
                    Bedrooms = 4,
                    Bathrooms = 3,
                    SquareMeters = 280,
                    LotSize = 0,
                    YearBuilt = 2019,
                    PropertyType = "Penthouse",
                    Status = Active,
                    DrawDate = DateTime.Now.AddDays(45),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                },
                new House
                {
                    Title = "Beach House in Herzliya",
                    Description = "Exclusive beachfront property with private access",
                    Price = 4500000,
                    TicketPrice = 200,
                    TotalTickets = 22500,
                    TicketsSold = 0,
                    City = "Herzliya",
                    Address = "789 Marina Blvd",
                    Bedrooms = 6,
                    Bathrooms = 5,
                    SquareMeters = 420,
                    LotSize = 800,
                    YearBuilt = 2021,
                    PropertyType = "Beach House",
                    Status = Active,
                    DrawDate = DateTime.Now.AddDays(60),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                },
                new House
                {
                    Title = "Modern Apartment in Haifa",
                    Description = "Contemporary apartment with mountain views",
                    Price = 1800000,
                    TicketPrice = 75,
                    TotalTickets = 24000,
                    TicketsSold = 0,
                    City = "Haifa",
                    Address = "321 Carmel Ave",
                    Bedrooms = 3,
                    Bathrooms = 2,
                    SquareMeters = 180,
                    LotSize = 0,
                    YearBuilt = 2018,
                    PropertyType = "Apartment",
                    Status = Active,
                    DrawDate = DateTime.Now.AddDays(75),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                },
                new House
                {
                    Title = "Country Estate in Galilee",
                    Description = "Spacious estate surrounded by nature",
                    Price = 2800000,
                    TicketPrice = 120,
                    TotalTickets = 23333,
                    TicketsSold = 0,
                    City = "Galilee",
                    Address = "654 Nature Trail",
                    Bedrooms = 7,
                    Bathrooms = 6,
                    SquareMeters = 500,
                    LotSize = 2000,
                    YearBuilt = 2017,
                    PropertyType = "Estate",
                    Status = Active,
                    DrawDate = DateTime.Now.AddDays(90),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                }
            };

            await _context.Houses.AddRangeAsync(houses);
            await _context.SaveChangesAsync();

            // Add images for each house (1 main + 3 additional = 4 total per house)
            var houseImages = new List<HouseImage>();
            var imageUrls = new[]
            {
                "https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=800",
                "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800",
                "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=800",
                "https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3?w=800"
            };

            foreach (var house in houses)
            {
                for (int i = 0; i < 4; i++)
                {
                    houseImages.Add(new HouseImage
                    {
                        HouseId = house.Id,
                        ImageUrl = imageUrls[i],
                        AltText = $"{house.Title} - Image {i + 1}",
                        IsMain = i == 0,
                        DisplayOrder = i + 1,
                        MediaType = Image,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = "System",
                        UpdatedBy = "System"
                    });
                }
            }

            await _context.HouseImages.AddRangeAsync(houseImages);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Seeded {houses.Count} houses with {houseImages.Count} images successfully");
        }

        private async Task LogSeedingResultsAsync()
        {
            _logger.LogInformation("=== SEEDING RESULTS ===");
            
            var languageCount = await _context.Languages.CountAsync();
            var translationCount = await _context.Translations.CountAsync();
            var userCount = await _context.Users.CountAsync();
            var houseCount = await _context.Houses.CountAsync();
            var imageCount = await _context.HouseImages.CountAsync();

            _logger.LogInformation($"Languages: {languageCount}");
            _logger.LogInformation($"Translations: {translationCount}");
            _logger.LogInformation($"Users: {userCount}");
            _logger.LogInformation($"Houses: {houseCount}");
            _logger.LogInformation($"House Images: {imageCount}");

            // Check Polish translations specifically
            var polishTranslations = await _context.Translations.CountAsync(t => t.LanguageCode == "pl");
            _logger.LogInformation($"Polish Translations: {polishTranslations}");

            _logger.LogInformation("=== END RESULTS ===");
        }
    }
}

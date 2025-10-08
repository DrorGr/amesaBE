using Microsoft.EntityFrameworkCore;
using AmesaBackend.Models;
using System.Security.Cryptography;
using System.Text;

namespace AmesaBackend.Data
{
    public class DatabaseSeeder
    {
        private readonly AmesaDbContext _context;

        public DatabaseSeeder(AmesaDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            try
            {
                Console.WriteLine("🌱 Starting database seeding...");

                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Seed in order (respecting foreign key constraints)
                await SeedLanguagesAsync();
                await SaveChangesWithUtcFix();

                await SeedUsersAsync();
                await SaveChangesWithUtcFix();

                await SeedHousesAsync();
                await SaveChangesWithUtcFix();

                await SeedLotteryTicketsAsync();
                await SaveChangesWithUtcFix();

                await SeedLotteryDrawsAsync();
                await SaveChangesWithUtcFix();

                await SeedLotteryResultsAsync();
                await SaveChangesWithUtcFix();

                await SeedTranslationsAsync();
                await SaveChangesWithUtcFix();

                await SeedContentAsync();
                await SaveChangesWithUtcFix();

                await SeedPromotionsAsync();
                await SaveChangesWithUtcFix();

                await SeedSystemSettingsAsync();
                await SaveChangesWithUtcFix();
                Console.WriteLine("✅ Database seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during database seeding: {ex.Message}");
                throw;
            }
        }

        private async Task SaveChangesWithUtcFix()
        {
            // Ensure all DateTime values are UTC before saving
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime))
                    {
                        if (property.CurrentValue is DateTime dateTime)
                        {
                            if (dateTime.Kind == DateTimeKind.Unspecified)
                            {
                                property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                            }
                            else if (dateTime.Kind == DateTimeKind.Local)
                            {
                                property.CurrentValue = dateTime.ToUniversalTime();
                            }
                        }
                    }
                    else if (property.Metadata.ClrType == typeof(DateTime?))
                    {
                        var nullableDateTime = property.CurrentValue as DateTime?;
                        if (nullableDateTime.HasValue)
                        {
                            var dateTimeValue = nullableDateTime.Value;
                            if (dateTimeValue.Kind == DateTimeKind.Unspecified)
                            {
                                property.CurrentValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc);
                            }
                            else if (dateTimeValue.Kind == DateTimeKind.Local)
                            {
                                property.CurrentValue = dateTimeValue.ToUniversalTime();
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedLanguagesAsync()
        {
            // Clear existing languages to ensure we have the latest ones
            var existingLanguages = await _context.Languages.ToListAsync();
            if (existingLanguages.Any())
            {
                _context.Languages.RemoveRange(existingLanguages);
                await _context.SaveChangesAsync();
                Console.WriteLine("🗑️ Cleared existing languages");
            }

            var languages = new List<Language>
            {
                new Language { Code = "en", Name = "English", NativeName = "English", FlagUrl = "🇺🇸" },
                new Language { Code = "he", Name = "Hebrew", NativeName = "עברית", FlagUrl = "🇮🇱" },
                new Language { Code = "ar", Name = "Arabic", NativeName = "العربية", FlagUrl = "🇸🇦" },
                new Language { Code = "es", Name = "Spanish", NativeName = "Español", FlagUrl = "🇪🇸" },
                new Language { Code = "fr", Name = "French", NativeName = "Français", FlagUrl = "🇫🇷" },
                new Language { Code = "pl", Name = "Polish", NativeName = "Polski", FlagUrl = "🇵🇱" }
            };

            _context.Languages.AddRange(languages);
            Console.WriteLine("✅ Seeded 6 languages");
        }

        private async Task SeedUsersAsync()
        {
            if (await _context.Users.AnyAsync())
            {
                Console.WriteLine("👥 Users already seeded, skipping...");
                return;
            }

            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Email = "admin@amesa.com",
                    EmailVerified = true,
                    Phone = "+972501234567",
                    PhoneVerified = true,
                    PasswordHash = HashPassword("Admin123!"),
                    FirstName = "Admin",
                    LastName = "User",
                    DateOfBirth = new DateTime(1985, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                    Gender = GenderType.Male,
                    IdNumber = "123456789",
                    Status = UserStatus.Active,
                    VerificationStatus = UserVerificationStatus.FullyVerified,
                    AuthProvider = AuthProvider.Email,
                    PreferredLanguage = "en",
                    Timezone = "Asia/Jerusalem",
                    LastLoginAt = DateTime.UtcNow.AddHours(-2),
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "john_doe",
                    Email = "john.doe@example.com",
                    EmailVerified = true,
                    Phone = "+972501234568",
                    PhoneVerified = true,
                    PasswordHash = HashPassword("Password123!"),
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateTime(1990, 8, 22, 0, 0, 0, DateTimeKind.Utc),
                    Gender = GenderType.Male,
                    IdNumber = "987654321",
                    Status = UserStatus.Active,
                    VerificationStatus = UserVerificationStatus.FullyVerified,
                    AuthProvider = AuthProvider.Email,
                    PreferredLanguage = "en",
                    Timezone = "Asia/Jerusalem",
                    LastLoginAt = DateTime.UtcNow.AddHours(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddHours(-1)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "sarah_wilson",
                    Email = "sarah.wilson@example.com",
                    EmailVerified = true,
                    Phone = "+972501234569",
                    PhoneVerified = false,
                    PasswordHash = HashPassword("Password123!"),
                    FirstName = "Sarah",
                    LastName = "Wilson",
                    DateOfBirth = new DateTime(1988, 12, 3, 0, 0, 0, DateTimeKind.Utc),
                    Gender = GenderType.Female,
                    IdNumber = "456789123",
                    Status = UserStatus.Active,
                    VerificationStatus = UserVerificationStatus.EmailVerified,
                    AuthProvider = AuthProvider.Email,
                    PreferredLanguage = "he",
                    Timezone = "Asia/Jerusalem",
                    LastLoginAt = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "ahmed_hassan",
                    Email = "ahmed.hassan@example.com",
                    EmailVerified = true,
                    Phone = "+972501234570",
                    PhoneVerified = true,
                    PasswordHash = HashPassword("Password123!"),
                    FirstName = "Ahmed",
                    LastName = "Hassan",
                    DateOfBirth = new DateTime(1992, 3, 18, 0, 0, 0, DateTimeKind.Utc),
                    Gender = GenderType.Male,
                    IdNumber = "789123456",
                    Status = UserStatus.Active,
                    VerificationStatus = UserVerificationStatus.FullyVerified,
                    AuthProvider = AuthProvider.Email,
                    PreferredLanguage = "ar",
                    Timezone = "Asia/Jerusalem",
                    LastLoginAt = DateTime.UtcNow.AddHours(-3),
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow.AddHours(-3)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "maria_garcia",
                    Email = "maria.garcia@example.com",
                    EmailVerified = false,
                    Phone = "+972501234571",
                    PhoneVerified = false,
                    PasswordHash = HashPassword("Password123!"),
                    FirstName = "Maria",
                    LastName = "Garcia",
                    DateOfBirth = new DateTime(1995, 7, 25, 0, 0, 0, DateTimeKind.Utc),
                    Gender = GenderType.Female,
                    IdNumber = "321654987",
                    Status = UserStatus.Pending,
                    VerificationStatus = UserVerificationStatus.Unverified,
                    AuthProvider = AuthProvider.Email,
                    PreferredLanguage = "es",
                    Timezone = "Asia/Jerusalem",
                    LastLoginAt = null,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            _context.Users.AddRange(users);

            // Add user addresses
            var userAddresses = new List<UserAddress>
            {
                new UserAddress
                {
                    Id = Guid.NewGuid(),
                    UserId = users[0].Id,
                    Type = "home",
                    Country = "Israel",
                    City = "Tel Aviv",
                    Street = "Rothschild Boulevard",
                    HouseNumber = "15",
                    ZipCode = "6688119",
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new UserAddress
                {
                    Id = Guid.NewGuid(),
                    UserId = users[1].Id,
                    Type = "home",
                    Country = "Israel",
                    City = "Jerusalem",
                    Street = "King George Street",
                    HouseNumber = "42",
                    ZipCode = "9100000",
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new UserAddress
                {
                    Id = Guid.NewGuid(),
                    UserId = users[2].Id,
                    Type = "home",
                    Country = "Israel",
                    City = "Haifa",
                    Street = "Herzl Street",
                    HouseNumber = "88",
                    ZipCode = "3100000",
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            _context.UserAddresses.AddRange(userAddresses);

            // Add user phones
            var userPhones = new List<UserPhone>
            {
                new UserPhone
                {
                    Id = Guid.NewGuid(),
                    UserId = users[0].Id,
                    PhoneNumber = "+972501234567",
                    IsPrimary = true,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new UserPhone
                {
                    Id = Guid.NewGuid(),
                    UserId = users[1].Id,
                    PhoneNumber = "+972501234568",
                    IsPrimary = true,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new UserPhone
                {
                    Id = Guid.NewGuid(),
                    UserId = users[2].Id,
                    PhoneNumber = "+972501234569",
                    IsPrimary = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            _context.UserPhones.AddRange(userPhones);

            Console.WriteLine("✅ Seeded 5 users with addresses and phones");
        }

        private async Task SeedHousesAsync()
        {
            // Clear existing houses and images to ensure we have the latest ones
            var existingHouses = await _context.Houses.ToListAsync();
            if (existingHouses.Any())
            {
                // Remove house images first
                var existingImages = await _context.HouseImages.ToListAsync();
                if (existingImages.Any())
                {
                    _context.HouseImages.RemoveRange(existingImages);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("🗑️ Cleared existing house images");
                }
                
                _context.Houses.RemoveRange(existingHouses);
                await _context.SaveChangesAsync();
                Console.WriteLine("🗑️ Cleared existing houses");
            }

            var users = await _context.Users.ToListAsync();
            var adminUser = users.FirstOrDefault(u => u.Username == "admin");

            var houses = new List<House>
            {
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Luxury Villa in Warsaw",
                    Description = "Stunning 4-bedroom villa with panoramic city views, private garden, and modern amenities. Located in the prestigious Mokotów district of Warsaw.",
                    Price = 1200000.00m,
                    Location = "Warsaw, Poland",
                    Address = "15 Ulica Puławska, Mokotów, Warsaw",
                    Bedrooms = 4,
                    Bathrooms = 3,
                    SquareFeet = 3500,
                    PropertyType = "Villa",
                    YearBuilt = 2020,
                    LotSize = 0.5m,
                    Features = new string[] { "Private Garden", "City View", "Parking", "Security System", "Air Conditioning", "Modern Kitchen" },
                    Status = LotteryStatus.Active,
                    TotalTickets = 60000,
                    TicketPrice = 20.00m,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-10),
                    LotteryEndDate = DateTime.UtcNow.AddDays(20),
                    DrawDate = DateTime.UtcNow.AddDays(21),
                    MinimumParticipationPercentage = 75.00m,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Modern Apartment in Kraków",
                    Description = "Contemporary 3-bedroom apartment in the heart of Kraków's Old Town. Features floor-to-ceiling windows, smart home technology, and access to rooftop amenities.",
                    Price = 800000.00m,
                    Location = "Kraków, Poland",
                    Address = "42 Rynek Główny, Kraków",
                    Bedrooms = 3,
                    Bathrooms = 2,
                    SquareFeet = 1200,
                    PropertyType = "Apartment",
                    YearBuilt = 2022,
                    LotSize = 0.1m,
                    Features = new string[] { "Smart Home", "Rooftop Access", "Gym", "Parking", "Balcony", "Old Town View" },
                    Status = LotteryStatus.Upcoming,
                    TotalTickets = 40000,
                    TicketPrice = 20.00m,
                    LotteryStartDate = DateTime.UtcNow.AddDays(5),
                    LotteryEndDate = DateTime.UtcNow.AddDays(35),
                    DrawDate = DateTime.UtcNow.AddDays(36),
                    MinimumParticipationPercentage = 80.00m,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Historic House in Gdańsk",
                    Description = "Beautifully restored 5-bedroom historic house in the Old Town of Gdańsk. Combines traditional architecture with modern comforts.",
                    Price = 900000.00m,
                    Location = "Gdańsk, Poland",
                    Address = "8 Długi Targ, Old Town, Gdańsk",
                    Bedrooms = 5,
                    Bathrooms = 4,
                    SquareFeet = 2800,
                    PropertyType = "Historic House",
                    YearBuilt = 1890,
                    LotSize = 0.3m,
                    Features = new string[] { "Historic Architecture", "Garden", "Terrace", "Parking", "Security", "Restored" },
                    Status = LotteryStatus.Ended,
                    TotalTickets = 45000,
                    TicketPrice = 20.00m,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-60),
                    LotteryEndDate = DateTime.UtcNow.AddDays(-30),
                    DrawDate = DateTime.UtcNow.AddDays(-29),
                    MinimumParticipationPercentage = 70.00m,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-75),
                    UpdatedAt = DateTime.UtcNow.AddDays(-29)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Beachfront Condo in Sopot",
                    Description = "Luxurious 2-bedroom beachfront condo with direct beach access, infinity pool, and Baltic Sea views.",
                    Price = 600000.00m,
                    Location = "Sopot, Poland",
                    Address = "25 Molo, Sopot",
                    Bedrooms = 2,
                    Bathrooms = 2,
                    SquareFeet = 900,
                    PropertyType = "Condo",
                    YearBuilt = 2021,
                    LotSize = 0.05m,
                    Features = new string[] { "Beach Access", "Infinity Pool", "Sea View", "Balcony", "Parking", "Concierge" },
                    Status = LotteryStatus.Active,
                    TotalTickets = 30000,
                    TicketPrice = 20.00m,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-5),
                    LotteryEndDate = DateTime.UtcNow.AddDays(25),
                    DrawDate = DateTime.UtcNow.AddDays(26),
                    MinimumParticipationPercentage = 85.00m,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Mountain Villa in Zakopane",
                    Description = "Spectacular 5-bedroom villa perched in the Tatra Mountains with breathtaking views of the peaks. Features traditional wooden architecture with modern luxury.",
                    Price = 1000000.00m,
                    Location = "Zakopane, Poland",
                    Address = "12 Krupówki, Zakopane",
                    Bedrooms = 5,
                    Bathrooms = 4,
                    SquareFeet = 3200,
                    PropertyType = "Villa",
                    YearBuilt = 2019,
                    LotSize = 0.8m,
                    Features = new string[] { "Mountain View", "Wooden Architecture", "Garden", "Fireplace", "Parking", "Ski Storage" },
                    Status = LotteryStatus.Active,
                    TotalTickets = 50000,
                    TicketPrice = 20.00m,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-3),
                    LotteryEndDate = DateTime.UtcNow.AddDays(27),
                    DrawDate = DateTime.UtcNow.AddDays(28),
                    MinimumParticipationPercentage = 80.00m,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Modern Penthouse in Wrocław",
                    Description = "Stunning 4-bedroom penthouse with panoramic views of Wrocław's Old Town and the Oder River. Features floor-to-ceiling windows and a private rooftop terrace.",
                    Price = 700000.00m,
                    Location = "Wrocław, Poland",
                    Address = "88 Rynek, Wrocław",
                    Bedrooms = 4,
                    Bathrooms = 3,
                    SquareFeet = 1800,
                    PropertyType = "Penthouse",
                    YearBuilt = 2023,
                    LotSize = 0.2m,
                    Features = new string[] { "River View", "Rooftop Terrace", "Smart Home", "Elevator", "Parking", "Concierge" },
                    Status = LotteryStatus.Upcoming,
                    TotalTickets = 35000,
                    TicketPrice = 20.00m,
                    LotteryStartDate = DateTime.UtcNow.AddDays(7),
                    LotteryEndDate = DateTime.UtcNow.AddDays(37),
                    DrawDate = DateTime.UtcNow.AddDays(38),
                    MinimumParticipationPercentage = 85.00m,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Lake House in Mazury",
                    Description = "Unique 3-bedroom lake house with private dock and stunning lake views. Perfect for those seeking tranquility and luxury by the water.",
                    Price = 500000.00m,
                    Location = "Mazury, Poland",
                    Address = "45 Lake View Road, Mazury",
                    Bedrooms = 3,
                    Bathrooms = 3,
                    SquareFeet = 1500,
                    PropertyType = "Villa",
                    YearBuilt = 2020,
                    LotSize = 0.6m,
                    Features = new string[] { "Lake View", "Private Dock", "Water Access", "Solar Panels", "Parking", "Outdoor Kitchen" },
                    Status = LotteryStatus.Active,
                    TotalTickets = 25000,
                    TicketPrice = 20.00m,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-2),
                    LotteryEndDate = DateTime.UtcNow.AddDays(28),
                    DrawDate = DateTime.UtcNow.AddDays(29),
                    MinimumParticipationPercentage = 75.00m,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Historic Mansion in Poznań",
                    Description = "Magnificent 6-bedroom historic mansion in the heart of Poznań's Old Town. Restored to perfection with original architectural details and modern amenities.",
                    Price = 1100000.00m,
                    Location = "Poznań, Poland",
                    Address = "3 Stary Rynek, Poznań",
                    Bedrooms = 6,
                    Bathrooms = 5,
                    SquareFeet = 4200,
                    PropertyType = "Historic Mansion",
                    YearBuilt = 1875,
                    LotSize = 0.4m,
                    Features = new string[] { "Historic Architecture", "Old Town Location", "Garden", "Terrace", "Parking", "Restored" },
                    Status = LotteryStatus.Upcoming,
                    TotalTickets = 55000,
                    TicketPrice = 20.00m,
                    LotteryStartDate = DateTime.UtcNow.AddDays(10),
                    LotteryEndDate = DateTime.UtcNow.AddDays(40),
                    DrawDate = DateTime.UtcNow.AddDays(41),
                    MinimumParticipationPercentage = 70.00m,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _context.Houses.AddRange(houses);

            // Add house images
            var houseImages = new List<HouseImage>
            {
                // Villa in Warsaw - 1 main large image + 4 small images
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[0].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=1200",
                    AltText = "Luxury Villa in Warsaw - Main Exterior View",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    MediaType = MediaType.Image,
                    FileSize = 2048000,
                    Width = 1920,
                    Height = 1080,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[0].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400",
                    AltText = "Luxury Villa - Living Room",
                    DisplayOrder = 2,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 475000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[0].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400",
                    AltText = "Luxury Villa - Swimming Pool",
                    DisplayOrder = 3,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 550000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[0].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400",
                    AltText = "Luxury Villa - Master Bedroom",
                    DisplayOrder = 4,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 525000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[0].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400",
                    AltText = "Luxury Villa - Kitchen",
                    DisplayOrder = 5,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 490000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },

                // Apartment in Kraków - 1 main large image + 4 small images
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[1].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200",
                    AltText = "Modern Apartment in Kraków - Main Living Room",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    MediaType = MediaType.Image,
                    FileSize = 1960000,
                    Width = 1920,
                    Height = 1080,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[1].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400",
                    AltText = "Modern Apartment - Kitchen",
                    DisplayOrder = 2,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 460000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[1].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400",
                    AltText = "Modern Apartment - Bedroom",
                    DisplayOrder = 3,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 475000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[1].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400",
                    AltText = "Modern Apartment - Rooftop View",
                    DisplayOrder = 4,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 550000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[1].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400",
                    AltText = "Modern Apartment - City View",
                    DisplayOrder = 5,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 500000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },

                // Historic House in Gdańsk - 1 main large image + 4 small images
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[2].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=1200",
                    AltText = "Historic House in Gdańsk - Main Exterior",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    MediaType = MediaType.Image,
                    FileSize = 2100000,
                    Width = 1920,
                    Height = 1080,
                    CreatedAt = DateTime.UtcNow.AddDays(-75)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[2].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400",
                    AltText = "Historic House - Interior",
                    DisplayOrder = 2,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 550000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-75)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[2].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400",
                    AltText = "Historic House - Bedroom",
                    DisplayOrder = 3,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 500000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-75)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[2].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400",
                    AltText = "Historic House - Traditional Kitchen",
                    DisplayOrder = 4,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 475000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-75)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[2].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400",
                    AltText = "Historic House - Garden",
                    DisplayOrder = 5,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 540000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-75)
                },

                // Beachfront Condo in Sopot - 1 main large image + 4 small images
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[3].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=1200",
                    AltText = "Beachfront Condo in Sopot - Main Sea View",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    MediaType = MediaType.Image,
                    FileSize = 2300000,
                    Width = 1920,
                    Height = 1080,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[3].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400",
                    AltText = "Beachfront Condo - Infinity Pool",
                    DisplayOrder = 2,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 540000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[3].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400",
                    AltText = "Beachfront Condo - Living Room",
                    DisplayOrder = 3,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 490000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[3].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400",
                    AltText = "Beachfront Condo - Bedroom",
                    DisplayOrder = 4,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 510000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[3].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400",
                    AltText = "Beachfront Condo - Balcony View",
                    DisplayOrder = 5,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 550000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },

                // Mountain Villa in Zakopane - 1 main large image + 4 small images
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[4].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=1200",
                    AltText = "Mountain Villa in Zakopane - Main Exterior View",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    MediaType = MediaType.Image,
                    FileSize = 2400000,
                    Width = 1920,
                    Height = 1080,
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[4].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=400",
                    AltText = "Mountain Villa - Wooden Architecture",
                    DisplayOrder = 2,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 575000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[4].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400",
                    AltText = "Mountain Villa - Living Room with Fireplace",
                    DisplayOrder = 3,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 540000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[4].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400",
                    AltText = "Mountain Villa - Kitchen",
                    DisplayOrder = 4,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 490000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[4].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400",
                    AltText = "Mountain Villa - Garden View",
                    DisplayOrder = 5,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 550000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },

                // Modern Penthouse in Wrocław - 1 main large image + 4 small images
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[5].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200",
                    AltText = "Modern Penthouse in Wrocław - Main Living Room",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    MediaType = MediaType.Image,
                    FileSize = 2360000,
                    Width = 1920,
                    Height = 1080,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[5].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400",
                    AltText = "Modern Penthouse - City View",
                    DisplayOrder = 2,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 600000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[5].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400",
                    AltText = "Modern Penthouse - Master Bedroom",
                    DisplayOrder = 3,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 525000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[5].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400",
                    AltText = "Modern Penthouse - Rooftop Terrace",
                    DisplayOrder = 4,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 560000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[5].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400",
                    AltText = "Modern Penthouse - Kitchen",
                    DisplayOrder = 5,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 490000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },

                // Lake House in Mazury - 1 main large image + 4 small images
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[6].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=1200",
                    AltText = "Lake House in Mazury - Main Exterior View",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    MediaType = MediaType.Image,
                    FileSize = 2500000,
                    Width = 1920,
                    Height = 1080,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[6].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400",
                    AltText = "Lake House - Private Dock",
                    DisplayOrder = 2,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 590000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[6].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400",
                    AltText = "Lake House - Living Room",
                    DisplayOrder = 3,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 540000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[6].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400",
                    AltText = "Lake House - Bedroom",
                    DisplayOrder = 4,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 510000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[6].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400",
                    AltText = "Lake House - Lake View",
                    DisplayOrder = 5,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 600000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },

                // Historic Mansion in Poznań - 1 main large image + 4 small images
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[7].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=1200",
                    AltText = "Historic Mansion in Poznań - Main Exterior",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    MediaType = MediaType.Image,
                    FileSize = 2600000,
                    Width = 1920,
                    Height = 1080,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[7].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400",
                    AltText = "Historic Mansion - Grand Living Room",
                    DisplayOrder = 2,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 625000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[7].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400",
                    AltText = "Historic Mansion - Master Suite",
                    DisplayOrder = 3,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 575000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[7].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400",
                    AltText = "Historic Mansion - Garden Terrace",
                    DisplayOrder = 4,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 600000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new HouseImage
                {
                    Id = Guid.NewGuid(),
                    HouseId = houses[7].Id,
                    ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400",
                    AltText = "Historic Mansion - Kitchen",
                    DisplayOrder = 5,
                    IsPrimary = false,
                    MediaType = MediaType.Image,
                    FileSize = 550000,
                    Width = 600,
                    Height = 400,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            };

            _context.HouseImages.AddRange(houseImages);

            Console.WriteLine("✅ Seeded 8 houses with 5 images each (1 main large + 4 small)");
        }

        private async Task SeedLotteryTicketsAsync()
        {
            if (await _context.LotteryTickets.AnyAsync())
            {
                Console.WriteLine("🎫 Lottery tickets already seeded, skipping...");
                return;
            }

            var users = await _context.Users.ToListAsync();
            var houses = await _context.Houses.ToListAsync();

            var tickets = new List<LotteryTicket>();
            var random = new Random();

            // Generate tickets for active lotteries
            foreach (var house in houses.Where(h => h.Status == LotteryStatus.Active || h.Status == LotteryStatus.Ended))
            {
                var ticketsForHouse = random.Next(50, 200); // Random number of tickets sold
                
                for (int i = 0; i < ticketsForHouse; i++)
                {
                    var user = users[random.Next(users.Count)];
                    var ticketNumber = $"{house.Id.ToString().Substring(0, 8).ToUpper()}-{i + 1:D4}";
                    
                    tickets.Add(new LotteryTicket
                    {
                        Id = Guid.NewGuid(),
                        TicketNumber = ticketNumber,
                        HouseId = house.Id,
                        UserId = user.Id,
                        PurchasePrice = house.TicketPrice,
                        Status = house.Status == LotteryStatus.Ended ? TicketStatus.Active : TicketStatus.Active,
                        PurchaseDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                    });
                }
            }

            _context.LotteryTickets.AddRange(tickets);

            // Create corresponding transactions
            var transactions = tickets.Select(ticket => new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = ticket.UserId,
                Type = TransactionType.TicketPurchase,
                Amount = ticket.PurchasePrice,
                Currency = "USD",
                Status = PaymentStatus.Completed,
                Description = $"Lottery ticket purchase - {ticket.TicketNumber}",
                ReferenceId = ticket.TicketNumber,
                ProcessedAt = ticket.PurchaseDate,
                CreatedAt = ticket.PurchaseDate,
                UpdatedAt = ticket.PurchaseDate
            }).ToList();

            _context.Transactions.AddRange(transactions);

            Console.WriteLine($"✅ Seeded {tickets.Count} lottery tickets with transactions");
        }

        private async Task SeedLotteryDrawsAsync()
        {
            if (await _context.LotteryDraws.AnyAsync())
            {
                Console.WriteLine("🎲 Lottery draws already seeded, skipping...");
                return;
            }

            var houses = await _context.Houses.Where(h => h.Status == LotteryStatus.Ended).ToListAsync();
            var tickets = await _context.LotteryTickets.ToListAsync();

            var draws = new List<LotteryDraw>();

            foreach (var house in houses)
            {
                var houseTickets = tickets.Where(t => t.HouseId == house.Id).ToList();
                var totalTicketsSold = houseTickets.Count;
                var participationPercentage = totalTicketsSold > 0 ? (decimal)totalTicketsSold / house.TotalTickets * 100 : 0;

                // Select a random winning ticket
                var winningTicket = houseTickets.Count > 0 ? houseTickets[new Random().Next(houseTickets.Count)] : null;

                draws.Add(new LotteryDraw
                {
                    Id = Guid.NewGuid(),
                    HouseId = house.Id,
                    DrawDate = house.DrawDate ?? DateTime.UtcNow.AddDays(-29),
                    TotalTicketsSold = totalTicketsSold,
                    TotalParticipationPercentage = participationPercentage,
                    WinningTicketNumber = winningTicket?.TicketNumber,
                    WinningTicketId = winningTicket?.Id,
                    WinnerUserId = winningTicket?.UserId,
                    DrawStatus = DrawStatus.Completed,
                    DrawMethod = "random",
                    DrawSeed = Guid.NewGuid().ToString(),
                    ConductedBy = null, // Admin user ID would go here
                    ConductedAt = house.DrawDate ?? DateTime.UtcNow.AddDays(-29),
                    VerificationHash = Guid.NewGuid().ToString(),
                    CreatedAt = house.DrawDate ?? DateTime.UtcNow.AddDays(-29),
                    UpdatedAt = house.DrawDate ?? DateTime.UtcNow.AddDays(-29)
                });
            }

            _context.LotteryDraws.AddRange(draws);
            Console.WriteLine($"✅ Seeded {draws.Count} lottery draws");
        }

        private async Task SeedLotteryResultsAsync()
        {
            if (await _context.LotteryResults.AnyAsync())
            {
                Console.WriteLine("🏆 Lottery results already seeded, skipping...");
                return;
            }

            var draws = await _context.LotteryDraws.Where(d => d.DrawStatus == DrawStatus.Completed).ToListAsync();
            var houses = await _context.Houses.ToListAsync();

            var results = new List<LotteryResult>();

            foreach (var draw in draws)
            {
                var house = houses.FirstOrDefault(h => h.Id == draw.HouseId);
                if (house == null || draw.WinnerUserId == null) continue;

                results.Add(new LotteryResult
                {
                    Id = Guid.NewGuid(),
                    LotteryId = house.Id,
                    DrawId = draw.Id,
                    WinnerTicketNumber = draw.WinningTicketNumber ?? "N/A",
                    WinnerUserId = draw.WinnerUserId.Value,
                    PrizePosition = 1,
                    PrizeType = "House",
                    PrizeValue = house.Price,
                    PrizeDescription = $"Winner of {house.Title} - {house.Description}",
                    QRCodeData = $"AMESA-WINNER-{draw.Id}-{DateTime.UtcNow:yyyyMMdd}",
                    QRCodeImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=AMESA-WINNER-{draw.Id}",
                    IsVerified = true,
                    IsClaimed = false,
                    ResultDate = draw.ConductedAt ?? DateTime.UtcNow.AddDays(-29),
                    CreatedAt = draw.ConductedAt ?? DateTime.UtcNow.AddDays(-29),
                    UpdatedAt = draw.ConductedAt ?? DateTime.UtcNow.AddDays(-29)
                });
            }

            _context.LotteryResults.AddRange(results);

            // Add result history
            var histories = results.Select(result => new LotteryResultHistory
            {
                Id = Guid.NewGuid(),
                LotteryResultId = result.Id,
                Action = "Created",
                Details = $"Lottery result created for winner {result.WinnerTicketNumber}",
                PerformedBy = "System",
                Timestamp = result.CreatedAt,
                IpAddress = "127.0.0.1",
                UserAgent = "DatabaseSeeder/1.0"
            }).ToList();

            _context.LotteryResultHistory.AddRange(histories);

            Console.WriteLine($"✅ Seeded {results.Count} lottery results with history");
        }

        private async Task SeedTranslationsAsync()
        {
            // Clear existing translations to ensure we have the latest ones
            var existingTranslations = await _context.Translations.ToListAsync();
            if (existingTranslations.Any())
            {
                _context.Translations.RemoveRange(existingTranslations);
                await _context.SaveChangesAsync();
                Console.WriteLine("🗑️ Cleared existing translations");
            }

            // Get comprehensive translations from the new translation files
            var translations = new List<Translation>();
            
            // Add English translations
            translations.AddRange(ComprehensiveTranslations.GetAllTranslations());
            
            // Add Polish translations
            translations.AddRange(PolishTranslations.GetPolishTranslations());

            _context.Translations.AddRange(translations);
            Console.WriteLine($"🌐 Seeded {translations.Count} comprehensive translations (English + Polish)");
        }

        private async Task SeedContentAsync()
        {
            if (await _context.Contents.AnyAsync())
            {
                Console.WriteLine("📄 Content already seeded, skipping...");
                return;
            }

            // First create content categories
            var categories = new List<ContentCategory>
            {
                new ContentCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "How It Works",
                    Slug = "how-it-works",
                    Description = "Information about how the lottery system works",
                    ParentId = null,
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ContentCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "About Us",
                    Slug = "about-us",
                    Description = "Information about Amesa Lottery company",
                    ParentId = null,
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ContentCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "FAQ",
                    Slug = "faq",
                    Description = "Frequently asked questions",
                    ParentId = null,
                    DisplayOrder = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.ContentCategories.AddRange(categories);

            var users = await _context.Users.ToListAsync();
            var adminUser = users.FirstOrDefault(u => u.Username == "admin");

            var contents = new List<Content>
            {
                new Content
                {
                    Id = Guid.NewGuid(),
                    Title = "How Amesa Lottery Works",
                    Slug = "how-amesa-lottery-works",
                    ContentBody = "Amesa Lottery is a unique property lottery system that allows participants to win luxury homes through ticket purchases. Here's how it works:\n\n1. **Browse Properties**: Explore our selection of premium properties\n2. **Purchase Tickets**: Buy tickets for the properties you're interested in\n3. **Wait for Draw**: Once the lottery closes, we conduct a fair random draw\n4. **Win Your Dream Home**: The winner receives the property as their prize\n\nOur system ensures transparency and fairness in every draw.",
                    Excerpt = "Learn how Amesa Lottery works and how you can win your dream home.",
                    CategoryId = categories[0].Id,
                    AuthorId = adminUser?.Id,
                    Language = "en",
                    Status = ContentStatus.Published,
                    PublishedAt = DateTime.UtcNow.AddDays(-30),
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Content
                {
                    Id = Guid.NewGuid(),
                    Title = "About Amesa Lottery",
                    Slug = "about-amesa-lottery",
                    ContentBody = "Amesa Lottery is a revolutionary property lottery platform that makes luxury home ownership accessible to everyone. Founded in 2024, we believe that everyone deserves a chance to own their dream home.\n\n**Our Mission**: To democratize luxury property ownership through fair and transparent lottery systems.\n\n**Our Values**:\n- Transparency in all operations\n- Fairness in every draw\n- Security for all participants\n- Innovation in lottery technology\n\nWe are licensed and regulated, ensuring that all our operations meet the highest standards of integrity and security.",
                    Excerpt = "Learn about Amesa Lottery's mission to democratize luxury property ownership.",
                    CategoryId = categories[1].Id,
                    AuthorId = adminUser?.Id,
                    Language = "en",
                    Status = ContentStatus.Published,
                    PublishedAt = DateTime.UtcNow.AddDays(-25),
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow.AddDays(-25)
                },
                new Content
                {
                    Id = Guid.NewGuid(),
                    Title = "Frequently Asked Questions",
                    Slug = "frequently-asked-questions",
                    ContentBody = "**Q: How do I participate in a lottery?**\nA: Simply browse our available properties, select the one you're interested in, and purchase tickets.\n\n**Q: When are the draws conducted?**\nA: Draws are conducted on the specified draw date for each lottery, usually after the lottery period ends.\n\n**Q: How is the winner selected?**\nA: Winners are selected through a secure, random number generation process that is audited and verified.\n\n**Q: What happens if I win?**\nA: If you win, you'll be contacted immediately and provided with instructions for claiming your prize.\n\n**Q: Are the lotteries legal?**\nA: Yes, all our lotteries are conducted in compliance with local regulations and are fully licensed.",
                    Excerpt = "Find answers to common questions about Amesa Lottery.",
                    CategoryId = categories[2].Id,
                    AuthorId = adminUser?.Id,
                    Language = "en",
                    Status = ContentStatus.Published,
                    PublishedAt = DateTime.UtcNow.AddDays(-20),
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow.AddDays(-20)
                }
            };

            _context.Contents.AddRange(contents);
            Console.WriteLine("✅ Seeded 3 content categories and 3 content articles");
        }

        private async Task SeedPromotionsAsync()
        {
            if (await _context.Promotions.AnyAsync())
            {
                Console.WriteLine("🎁 Promotions already seeded, skipping...");
                return;
            }

            var users = await _context.Users.ToListAsync();
            var adminUser = users.FirstOrDefault(u => u.Username == "admin");

            var promotions = new List<Promotion>
            {
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Title = "New User Welcome Bonus",
                    Description = "Get 10% off your first ticket purchase",
                    Type = "discount",
                    Value = 10.00m,
                    Code = "WELCOME10",
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    UsageLimit = 1000,
                    UsageCount = 0,
                    MinPurchaseAmount = 100.00m,
                    MaxDiscountAmount = 500.00m,
                    ApplicableHouses = null, // Applies to all houses
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Title = "Bulk Purchase Discount",
                    Description = "Buy 5 tickets and get 15% off",
                    Type = "bulk_discount",
                    Value = 15.00m,
                    Code = "BULK15",
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddDays(-15),
                    EndDate = DateTime.UtcNow.AddDays(45),
                    UsageLimit = 500,
                    UsageCount = 0,
                    MinPurchaseAmount = 1000.00m,
                    MaxDiscountAmount = 1000.00m,
                    ApplicableHouses = null,
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Title = "Luxury Villa Special",
                    Description = "Special discount for Herzliya Villa lottery",
                    Type = "house_specific",
                    Value = 20.00m,
                    Code = "VILLA20",
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddDays(-10),
                    EndDate = DateTime.UtcNow.AddDays(20),
                    UsageLimit = 100,
                    UsageCount = 0,
                    MinPurchaseAmount = 2000.00m,
                    MaxDiscountAmount = 500.00m,
                    ApplicableHouses = null, // Will be set to specific house IDs if needed
                    CreatedBy = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            _context.Promotions.AddRange(promotions);
            Console.WriteLine("✅ Seeded 3 promotional campaigns");
        }

        private async Task SeedSystemSettingsAsync()
        {
            if (await _context.SystemSettings.AnyAsync())
            {
                Console.WriteLine("⚙️ System settings already seeded, skipping...");
                return;
            }

            var settings = new List<SystemSetting>
            {
                new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "site_name",
                    Value = "Amesa Lottery",
                    Type = "string",
                    Description = "The name of the website",
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "site_description",
                    Value = "Win your dream home through fair and transparent lotteries",
                    Type = "string",
                    Description = "The description of the website",
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "default_language",
                    Value = "en",
                    Type = "string",
                    Description = "The default language for the website",
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "min_ticket_purchase",
                    Value = "1",
                    Type = "integer",
                    Description = "Minimum number of tickets that can be purchased",
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "max_ticket_purchase",
                    Value = "100",
                    Type = "integer",
                    Description = "Maximum number of tickets that can be purchased per user per lottery",
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "lottery_extension_hours",
                    Value = "24",
                    Type = "integer",
                    Description = "Number of hours to extend lottery if minimum participation is not met",
                    IsPublic = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "email_notifications_enabled",
                    Value = "true",
                    Type = "boolean",
                    Description = "Whether email notifications are enabled",
                    IsPublic = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "maintenance_mode",
                    Value = "false",
                    Type = "boolean",
                    Description = "Whether the site is in maintenance mode",
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.SystemSettings.AddRange(settings);
            Console.WriteLine("✅ Seeded 8 system settings");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "amesa_salt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

using AmesaBackend.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace AmesaBackend.Services
{
    public static class DataSeedingService
    {
        public static async Task SeedDatabaseAsync(AmesaDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await context.Houses.AnyAsync())
            {
                return; // Data already seeded
            }

            // Seed Houses with mock data
            var houses = new List<House>
            {
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Luxury Villa in Tuscany",
                    Description = "A stunning 5-bedroom villa with panoramic views of the Tuscan countryside. Features include a private pool, wine cellar, and olive grove.",
                    Price = 2500000,
                    Location = "Tuscany, Italy",
                    Address = "Via della Villa, 123, Tuscany",
                    Bedrooms = 5,
                    Bathrooms = 4,
                    SquareFeet = 4500,
                    PropertyType = "Villa",
                    YearBuilt = 2018,
                    LotSize = 2.5m,
                    Features = new string[] { "Private Pool", "Wine Cellar", "Olive Grove", "Mountain Views", "Fireplace" },
                    Status = LotteryStatus.Active,
                    TotalTickets = 10000,
                    TicketPrice = 250,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-30),
                    LotteryEndDate = DateTime.UtcNow.AddDays(15),
                    DrawDate = DateTime.UtcNow.AddDays(16),
                    MinimumParticipationPercentage = 75.0m,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Modern Penthouse in Manhattan",
                    Description = "Sophisticated penthouse with floor-to-ceiling windows, private terrace, and city skyline views. Located in the heart of Manhattan.",
                    Price = 3500000,
                    Location = "Manhattan, NY",
                    Address = "Central Park West, 456, New York, NY",
                    Bedrooms = 3,
                    Bathrooms = 3,
                    SquareFeet = 2800,
                    PropertyType = "Penthouse",
                    YearBuilt = 2020,
                    LotSize = 0.2m,
                    Features = new string[] { "Private Terrace", "City Views", "Concierge", "Gym", "Wine Storage" },
                    Status = LotteryStatus.Active,
                    TotalTickets = 15000,
                    TicketPrice = 350,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-20),
                    LotteryEndDate = DateTime.UtcNow.AddDays(25),
                    DrawDate = DateTime.UtcNow.AddDays(26),
                    MinimumParticipationPercentage = 80.0m,
                    CreatedAt = DateTime.UtcNow.AddDays(-45)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Beachfront Villa in Santorini",
                    Description = "Exclusive beachfront villa with infinity pool, private beach access, and breathtaking sunset views over the Aegean Sea.",
                    Price = 1800000,
                    Location = "Santorini, Greece",
                    Address = "Oia Village, Santorini",
                    Bedrooms = 4,
                    Bathrooms = 3,
                    SquareFeet = 3200,
                    PropertyType = "Villa",
                    YearBuilt = 2019,
                    LotSize = 1.8m,
                    Features = new string[] { "Infinity Pool", "Private Beach", "Sunset Views", "Outdoor Kitchen", "Wine Cave" },
                    Status = LotteryStatus.Active,
                    TotalTickets = 8000,
                    TicketPrice = 220,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-10),
                    LotteryEndDate = DateTime.UtcNow.AddDays(35),
                    DrawDate = DateTime.UtcNow.AddDays(36),
                    MinimumParticipationPercentage = 70.0m,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Mountain Lodge in Aspen",
                    Description = "Cozy mountain lodge with ski-in/ski-out access, hot tub, and panoramic mountain views. Perfect for winter sports enthusiasts.",
                    Price = 1200000,
                    Location = "Aspen, Colorado",
                    Address = "Mountain View Road, Aspen, CO",
                    Bedrooms = 4,
                    Bathrooms = 3,
                    SquareFeet = 2800,
                    PropertyType = "Lodge",
                    YearBuilt = 2017,
                    LotSize = 3.2m,
                    Features = new string[] { "Ski-in/Ski-out", "Hot Tub", "Mountain Views", "Fireplace", "Game Room" },
                    Status = LotteryStatus.Active,
                    TotalTickets = 6000,
                    TicketPrice = 200,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-5),
                    LotteryEndDate = DateTime.UtcNow.AddDays(40),
                    DrawDate = DateTime.UtcNow.AddDays(41),
                    MinimumParticipationPercentage = 75.0m,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Completed Lottery - London Townhouse",
                    Description = "Victorian townhouse in Kensington with original features, private garden, and modern amenities. This lottery has been completed.",
                    Price = 2200000,
                    Location = "London, UK",
                    Address = "Kensington Gardens, London",
                    Bedrooms = 4,
                    Bathrooms = 3,
                    SquareFeet = 2200,
                    PropertyType = "Townhouse",
                    YearBuilt = 1890,
                    LotSize = 0.8m,
                    Features = new string[] { "Private Garden", "Original Features", "Modern Kitchen", "High Ceilings", "Fireplace" },
                    Status = LotteryStatus.Completed,
                    TotalTickets = 12000,
                    TicketPrice = 280,
                    LotteryStartDate = DateTime.UtcNow.AddDays(-90),
                    LotteryEndDate = DateTime.UtcNow.AddDays(-10),
                    DrawDate = DateTime.UtcNow.AddDays(-5),
                    MinimumParticipationPercentage = 75.0m,
                    CreatedAt = DateTime.UtcNow.AddDays(-120)
                }
            };

            await context.Houses.AddRangeAsync(houses);

            // Add multiple placeholder images for each house with different images per property
            var houseImages = new List<HouseImage>();
            
            // Define different image sets for different property types
            var propertyImageSets = new Dictionary<string, (string[] urls, string[] altTexts)>
            {
                ["Mountain Lodge in Aspen"] = (new[]
                {
                    "https://images.unsplash.com/photo-1449824913935-59a10b8d2000?w=800&h=600&fit=crop&q=80", // Mountain lodge exterior
                    "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop&q=80", // Cozy mountain living room
                    "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=800&h=600&fit=crop&q=80", // Rustic kitchen
                    "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&h=600&fit=crop&q=80"  // Mountain view bedroom
                }, new[] { "Mountain exterior", "Cozy living room", "Rustic kitchen", "Mountain view bedroom" }),

                ["Beachfront Villa in Santorini"] = (new[]
                {
                    "https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=800&h=600&fit=crop&q=80", // Santorini villa exterior
                    "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800&h=600&fit=crop&q=80", // Ocean view living room
                    "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?w=800&h=600&fit=crop&q=80", // White kitchen with sea view
                    "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?w=800&h=600&fit=crop&q=80"  // Sunset view bedroom
                }, new[] { "Villa exterior", "Ocean view living room", "White kitchen", "Sunset bedroom" }),

                ["Modern Penthouse in Manhattan"] = (new[]
                {
                    "https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=800&h=600&fit=crop&q=80", // Modern penthouse exterior
                    "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&h=600&fit=crop&q=80", // City view living room
                    "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=800&h=600&fit=crop&q=80", // Modern kitchen
                    "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?w=800&h=600&fit=crop&q=80"  // Skyline view bedroom
                }, new[] { "Penthouse exterior", "City view living room", "Modern kitchen", "Skyline bedroom" }),

                ["Luxury Villa in Tuscany"] = (new[]
                {
                    "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800&h=600&fit=crop&q=80", // Tuscan villa exterior
                    "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&h=600&fit=crop&q=80", // Tuscan living room
                    "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=800&h=600&fit=crop&q=80", // Italian kitchen
                    "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?w=800&h=600&fit=crop&q=80"  // Tuscan countryside bedroom
                }, new[] { "Villa exterior", "Tuscan living room", "Italian kitchen", "Countryside bedroom" }),

                ["Completed Lottery - London Townhouse"] = (new[]
                {
                    "https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=800&h=600&fit=crop&q=80", // London townhouse exterior
                    "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&h=600&fit=crop&q=80", // Victorian living room
                    "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=800&h=600&fit=crop&q=80", // Modern kitchen
                    "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?w=800&h=600&fit=crop&q=80"  // Victorian bedroom
                }, new[] { "Townhouse exterior", "Victorian living room", "Modern kitchen", "Victorian bedroom" })
            };

            foreach (var house in houses)
            {
                if (propertyImageSets.TryGetValue(house.Title, out var imageSet))
                {
                    for (int i = 0; i < imageSet.urls.Length; i++)
                    {
                        houseImages.Add(new HouseImage
                        {
                            Id = Guid.NewGuid(),
                            HouseId = house.Id,
                            ImageUrl = imageSet.urls[i],
                            AltText = $"{house.Title} - {imageSet.altTexts[i]}",
                            DisplayOrder = i,
                            IsPrimary = i == 0, // First image is primary
                            MediaType = MediaType.Image,
                            FileSize = 250000,
                            Width = 800,
                            Height = 600,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    // Fallback images if house title doesn't match
                    var fallbackUrls = new[]
                    {
                        "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800&h=600&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&h=600&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=800&h=600&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?w=800&h=600&fit=crop&q=80"
                    };
                    var fallbackAlts = new[] { "Exterior view", "Living room", "Kitchen", "Bedroom" };

                    for (int i = 0; i < fallbackUrls.Length; i++)
                    {
                        houseImages.Add(new HouseImage
                        {
                            Id = Guid.NewGuid(),
                            HouseId = house.Id,
                            ImageUrl = fallbackUrls[i],
                            AltText = $"{house.Title} - {fallbackAlts[i]}",
                            DisplayOrder = i,
                            IsPrimary = i == 0,
                            MediaType = MediaType.Image,
                            FileSize = 250000,
                            Width = 800,
                            Height = 600,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await context.HouseImages.AddRangeAsync(houseImages);

            // Seed Users
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "john_doe",
                    Email = "john.doe@example.com",
                    EmailVerified = true,
                    Phone = "+1234567890",
                    PhoneVerified = true,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateTime(1985, 5, 15),
                    Gender = GenderType.Male,
                    AuthProvider = AuthProvider.Email,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    LastLoginAt = DateTime.UtcNow.AddHours(-2)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "jane_smith",
                    Email = "jane.smith@example.com",
                    EmailVerified = true,
                    Phone = "+1987654321",
                    PhoneVerified = true,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    FirstName = "Jane",
                    LastName = "Smith",
                    DateOfBirth = new DateTime(1990, 8, 22),
                    Gender = GenderType.Female,
                    AuthProvider = AuthProvider.Email,
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    LastLoginAt = DateTime.UtcNow.AddHours(-5)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "mike_wilson",
                    Email = "mike.wilson@example.com",
                    EmailVerified = true,
                    Phone = "+1555123456",
                    PhoneVerified = false,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    FirstName = "Mike",
                    LastName = "Wilson",
                    DateOfBirth = new DateTime(1978, 12, 10),
                    Gender = GenderType.Male,
                    AuthProvider = AuthProvider.Email,
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    LastLoginAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            await context.Users.AddRangeAsync(users);

            // Seed Lottery Tickets
            var tickets = new List<LotteryTicket>();
            var random = new Random();
            
            foreach (var house in houses.Take(4)) // Exclude completed lottery
            {
                var ticketsSold = house.Status == LotteryStatus.Active ? random.Next(house.TotalTickets / 2, house.TotalTickets) : house.TotalTickets;
                var ticketsForHouse = Enumerable.Range(1, ticketsSold)
                    .Select(i => new LotteryTicket
                    {
                        Id = Guid.NewGuid(),
                        HouseId = house.Id,
                        UserId = users[random.Next(users.Count)].Id,
                        TicketNumber = $"{house.Id.ToString().Substring(0, 8).ToUpper()}-{i:D6}",
                        PurchaseDate = DateTime.UtcNow.AddDays(-random.Next(30)),
                        PurchasePrice = house.TicketPrice,
                        Status = TicketStatus.Active,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(30)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(30))
                    }).ToList();
                
                tickets.AddRange(ticketsForHouse);
            }

            await context.LotteryTickets.AddRangeAsync(tickets);

            // Save all changes
            await context.SaveChangesAsync();
        }
    }
}

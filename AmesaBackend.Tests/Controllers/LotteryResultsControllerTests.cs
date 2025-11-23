using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using AmesaBackend.Data;
using AmesaBackend.Models;
using AmesaBackend.Services;
using AmesaBackend.DTOs;

namespace AmesaBackend.Tests.Controllers
{
    public class LotteryResultsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly AmesaDbContext _context;
        private readonly IServiceScope _scope;

        public LotteryResultsControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AmesaDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Add in-memory database
                    services.AddDbContext<AmesaDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                    });
                });
            });

            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<AmesaDbContext>();
            
            // Ensure database is created
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            
            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create test users
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "testuser1",
                    Email = "test1@example.com",
                    FirstName = "Test",
                    LastName = "User1",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Status = UserStatus.Active,
                    AuthProvider = AuthProvider.Email,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "testuser2",
                    Email = "test2@example.com",
                    FirstName = "Test",
                    LastName = "User2",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Status = UserStatus.Active,
                    AuthProvider = AuthProvider.Email,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);

            // Create test houses
            var houses = new List<House>
            {
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Test House 1",
                    Description = "A beautiful test house",
                    Price = 500000,
                    Location = "Test City",
                    Address = "123 Test Street",
                    Bedrooms = 3,
                    Bathrooms = 2,
                    SquareFeet = 1500,
                    PropertyType = "House",
                    Status = LotteryStatus.Active,
                    TotalTickets = 1000,
                    TicketPrice = 50,
                    LotteryEndDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow
                },
                new House
                {
                    Id = Guid.NewGuid(),
                    Title = "Test House 2",
                    Description = "Another beautiful test house",
                    Price = 750000,
                    Location = "Test City",
                    Address = "456 Test Avenue",
                    Bedrooms = 4,
                    Bathrooms = 3,
                    SquareFeet = 2000,
                    PropertyType = "House",
                    Status = LotteryStatus.Active,
                    TotalTickets = 1500,
                    TicketPrice = 75,
                    LotteryEndDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Houses.AddRange(houses);

            // Create test lottery results
            var qrCodeService = _scope.ServiceProvider.GetRequiredService<IQRCodeService>();
            var lotteryResults = new List<LotteryResult>();

            foreach (var house in houses)
            {
                for (int position = 1; position <= 3; position++)
                {
                    var winner = users[position % users.Count];
                    var ticketNumber = $"TICKET-{house.Id.ToString()[..8]}-{position}";
                    
                    var qrCodeData = qrCodeService.GenerateQRCodeDataAsync(
                        Guid.NewGuid(), 
                        ticketNumber, 
                        position
                    ).Result;

                    var prizeValue = position switch
                    {
                        1 => house.Price,
                        2 => house.Price * 0.1m,
                        3 => house.Price * 0.05m,
                        _ => 0
                    };

                    var lotteryResult = new LotteryResult
                    {
                        Id = Guid.NewGuid(),
                        LotteryId = house.Id,
                        DrawId = Guid.NewGuid(),
                        WinnerTicketNumber = ticketNumber,
                        WinnerUserId = winner.Id,
                        PrizePosition = position,
                        PrizeType = position == 1 ? "House" : "Cash",
                        PrizeValue = prizeValue,
                        PrizeDescription = position == 1 ? $"Winner of {house.Title}" : $"Cash prize: ${prizeValue:N0}",
                        QRCodeData = qrCodeData,
                        QRCodeImageUrl = qrCodeService.GenerateQRCodeImageUrl(qrCodeData),
                        IsVerified = true,
                        IsClaimed = position == 1, // Only first place is claimed
                        ClaimedAt = position == 1 ? DateTime.UtcNow.AddDays(-1) : null,
                        ResultDate = DateTime.UtcNow.AddDays(-position),
                        CreatedAt = DateTime.UtcNow.AddDays(-position),
                        UpdatedAt = DateTime.UtcNow.AddDays(-position)
                    };

                    lotteryResults.Add(lotteryResult);
                }
            }

            _context.LotteryResults.AddRange(lotteryResults);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetLotteryResults_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/lotteryresults");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetLotteryResults_ReturnsValidJson()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/lotteryresults");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultsPageDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Results.Count > 0);
        }

        [Fact]
        public async Task GetLotteryResults_WithFilters_ReturnsFilteredResults()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/lotteryresults?prizePosition=1&pageSize=5");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultsPageDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.All(result.Data.Results, r => Assert.Equal(1, r.PrizePosition));
        }

        [Fact]
        public async Task GetLotteryResult_WithValidId_ReturnsResult()
        {
            // Arrange
            var lotteryResult = await _context.LotteryResults.FirstAsync();

            // Act
            var response = await _client.GetAsync($"/api/v1/lotteryresults/{lotteryResult.Id}");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(lotteryResult.Id, result.Data.Id);
        }

        [Fact]
        public async Task GetLotteryResult_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync($"/api/v1/lotteryresults/{Guid.NewGuid()}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ValidateQRCode_WithValidCode_ReturnsSuccess()
        {
            // Arrange
            var lotteryResult = await _context.LotteryResults.FirstAsync();

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/lotteryresults/validate-qr", lotteryResult.QRCodeData);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<QRCodeValidationDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.IsValid);
            Assert.True(result.Data.IsWinner);
        }

        [Fact]
        public async Task ValidateQRCode_WithInvalidCode_ReturnsInvalid()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/lotteryresults/validate-qr", "invalid-qr-code");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<QRCodeValidationDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.False(result.Data.IsValid);
            Assert.False(result.Data.IsWinner);
        }

        [Fact]
        public async Task ClaimPrize_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var unclaimedResult = await _context.LotteryResults.FirstAsync(r => !r.IsClaimed);
            var claimRequest = new ClaimPrizeRequest
            {
                ResultId = unclaimedResult.Id,
                ClaimNotes = "Test claim"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/lotteryresults/claim", claimRequest);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.IsClaimed);
        }

        [Fact]
        public async Task ClaimPrize_WithAlreadyClaimed_ReturnsBadRequest()
        {
            // Arrange
            var claimedResult = await _context.LotteryResults.FirstAsync(r => r.IsClaimed);
            var claimRequest = new ClaimPrizeRequest
            {
                ResultId = claimedResult.Id,
                ClaimNotes = "Test claim"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/lotteryresults/claim", claimRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreatePrizeDelivery_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var lotteryResult = await _context.LotteryResults.FirstAsync(r => r.PrizePosition > 1);
            var deliveryRequest = new CreatePrizeDeliveryRequest
            {
                LotteryResultId = lotteryResult.Id,
                RecipientName = "Test Recipient",
                AddressLine1 = "123 Test Street",
                City = "Test City",
                State = "Test State",
                PostalCode = "12345",
                Country = "Test Country",
                DeliveryMethod = "Standard"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/lotteryresults/delivery", deliveryRequest);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<PrizeDeliveryDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(deliveryRequest.RecipientName, result.Data.RecipientName);
        }

        [Fact]
        public async Task CreatePrizeDelivery_WithFirstPlace_ReturnsBadRequest()
        {
            // Arrange
            var lotteryResult = await _context.LotteryResults.FirstAsync(r => r.PrizePosition == 1);
            var deliveryRequest = new CreatePrizeDeliveryRequest
            {
                LotteryResultId = lotteryResult.Id,
                RecipientName = "Test Recipient",
                AddressLine1 = "123 Test Street",
                City = "Test City",
                State = "Test State",
                PostalCode = "12345",
                Country = "Test Country",
                DeliveryMethod = "Standard"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/lotteryresults/delivery", deliveryRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        public void Dispose()
        {
            _context.Dispose();
            _scope.Dispose();
            _client.Dispose();
        }
    }
}



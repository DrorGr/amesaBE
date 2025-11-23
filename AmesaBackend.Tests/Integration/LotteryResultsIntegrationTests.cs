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

namespace AmesaBackend.Tests.Integration
{
    public class LotteryResultsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly AmesaDbContext _context;
        private readonly IServiceScope _scope;

        public LotteryResultsIntegrationTests(WebApplicationFactory<Program> factory)
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
                        options.UseInMemoryDatabase("IntegrationTestDb_" + Guid.NewGuid());
                    });
                });
            });

            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<AmesaDbContext>();
            
            // Seed comprehensive test data
            SeedIntegrationTestData();
        }

        private void SeedIntegrationTestData()
        {
            // Create test languages
            var languages = new List<Language>
            {
                new Language { Code = "en", Name = "English", NativeName = "English", FlagUrl = "us", IsActive = true, IsDefault = true, DisplayOrder = 1 },
                new Language { Code = "pl", Name = "Polish", NativeName = "Polski", FlagUrl = "pl", IsActive = true, IsDefault = false, DisplayOrder = 2 }
            };
            _context.Languages.AddRange(languages);

            // Create test users
            var users = new List<User>();
            for (int i = 1; i <= 10; i++)
            {
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"testuser{i}",
                    Email = $"test{i}@example.com",
                    FirstName = "Test",
                    LastName = $"User{i}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Status = UserStatus.Active,
                    AuthProvider = AuthProvider.Email,
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
            _context.Users.AddRange(users);

            // Create test houses
            var houses = new List<House>();
            for (int i = 1; i <= 5; i++)
            {
                houses.Add(new House
                {
                    Id = Guid.NewGuid(),
                    Title = $"Test House {i}",
                    Description = $"A beautiful test house number {i}",
                    Price = 500000 + (i * 100000),
                    Location = "Test City",
                    Address = $"{100 + i} Test Street",
                    Bedrooms = 3 + i,
                    Bathrooms = 2 + i,
                    SquareFeet = 1500 + (i * 200),
                    PropertyType = "House",
                    Status = LotteryStatus.Active,
                    TotalTickets = 1000 + (i * 200),
                    TicketPrice = 50 + (i * 10),
                    LotteryEndDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow.AddDays(-i * 2)
                });
            }
            _context.Houses.AddRange(houses);

            // Create lottery results with QR codes
            var qrCodeService = _scope.ServiceProvider.GetRequiredService<IQRCodeService>();
            var lotteryResults = new List<LotteryResult>();

            foreach (var house in houses)
            {
                for (int position = 1; position <= 3; position++)
                {
                    var winner = users[position % users.Count];
                    var ticketNumber = $"TICKET-{house.Id.ToString()[..8]}-{position:D3}";
                    
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
                        ClaimedAt = position == 1 ? DateTime.UtcNow.AddDays(-position) : null,
                        ResultDate = DateTime.UtcNow.AddDays(-position * 7),
                        CreatedAt = DateTime.UtcNow.AddDays(-position * 7),
                        UpdatedAt = DateTime.UtcNow.AddDays(-position * 7)
                    };

                    lotteryResults.Add(lotteryResult);

                    // Add history entries
                    var historyEntry = new LotteryResultHistory
                    {
                        Id = Guid.NewGuid(),
                        LotteryResultId = lotteryResult.Id,
                        Action = "Created",
                        Details = $"Lottery result created for {lotteryResult.PrizeDescription}",
                        PerformedBy = "System",
                        Timestamp = lotteryResult.CreatedAt,
                        IpAddress = "127.0.0.1",
                        UserAgent = "AmesaBackend/1.0"
                    };

                    _context.LotteryResultHistory.Add(historyEntry);

                    if (lotteryResult.IsClaimed)
                    {
                        var claimHistoryEntry = new LotteryResultHistory
                        {
                            Id = Guid.NewGuid(),
                            LotteryResultId = lotteryResult.Id,
                            Action = "Claimed",
                            Details = "Prize claimed by winner",
                            PerformedBy = winner.Email,
                            Timestamp = lotteryResult.ClaimedAt!.Value,
                            IpAddress = "127.0.0.1",
                            UserAgent = "AmesaFrontend/1.0"
                        };

                        _context.LotteryResultHistory.Add(claimHistoryEntry);
                    }
                }
            }

            _context.LotteryResults.AddRange(lotteryResults);

            // Create prize deliveries for some results
            var deliveries = new List<PrizeDelivery>();
            var random = new Random();
            
            foreach (var result in lotteryResults.Where(r => r.PrizePosition > 1 && r.IsClaimed))
            {
                var delivery = new PrizeDelivery
                {
                    Id = Guid.NewGuid(),
                    LotteryResultId = result.Id,
                    WinnerUserId = result.WinnerUserId,
                    RecipientName = $"Test Recipient {result.PrizePosition}",
                    AddressLine1 = $"{random.Next(100, 9999)} Main Street",
                    City = "Test City",
                    State = "Test State",
                    PostalCode = $"{random.Next(10000, 99999)}",
                    Country = "United States",
                    Phone = $"+1-555-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                    Email = $"test{result.PrizePosition}@example.com",
                    DeliveryMethod = "Standard",
                    DeliveryStatus = "Delivered",
                    EstimatedDeliveryDate = DateTime.UtcNow.AddDays(-random.Next(1, 7)),
                    ActualDeliveryDate = DateTime.UtcNow.AddDays(-random.Next(1, 3)),
                    ShippingCost = random.Next(10, 50),
                    DeliveryNotes = "Standard shipping via courier service",
                    CreatedAt = result.ClaimedAt!.Value,
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 7))
                };

                deliveries.Add(delivery);
            }

            _context.PrizeDeliveries.AddRange(deliveries);

            // Create scratch card results
            var scratchCards = new List<ScratchCardResult>();
            var scratchCardTypes = new[] { "Bronze", "Silver", "Gold", "Platinum" };

            for (int i = 0; i < 50; i++)
            {
                var user = users[random.Next(users.Count)];
                var cardType = scratchCardTypes[random.Next(scratchCardTypes.Length)];
                var isWinner = random.NextDouble() > 0.7; // 30% win rate

                var prizeValue = isWinner ? random.Next(10, 500) : 0;
                var prizeType = isWinner ? "Cash" : null;
                var prizeDescription = isWinner ? $"You won ${prizeValue}!" : "Better luck next time!";

                var scratchCard = new ScratchCardResult
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CardType = cardType,
                    CardNumber = $"SC-{cardType[0]}-{random.Next(100000, 999999)}",
                    IsWinner = isWinner,
                    PrizeType = prizeType,
                    PrizeValue = prizeValue,
                    PrizeDescription = prizeDescription,
                    CardImageUrl = $"https://via.placeholder.com/300x200/cccccc/666666?text={cardType}+Card",
                    ScratchedImageUrl = isWinner 
                        ? $"https://via.placeholder.com/300x200/4ade80/ffffff?text=WINNER%21%0A${prizeValue}"
                        : $"https://via.placeholder.com/300x200/ef4444/ffffff?text=Sorry%2C+Try+Again",
                    IsScratched = random.NextDouble() > 0.3, // 70% scratched
                    ScratchedAt = random.NextDouble() > 0.3 ? DateTime.UtcNow.AddDays(-random.Next(1, 30)) : null,
                    IsClaimed = isWinner && random.NextDouble() > 0.5,
                    ClaimedAt = isWinner && random.NextDouble() > 0.5 ? DateTime.UtcNow.AddDays(-random.Next(1, 20)) : null,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 60)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                };

                scratchCards.Add(scratchCard);
            }

            _context.ScratchCardResults.AddRange(scratchCards);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetLotteryResults_WithPagination_ReturnsCorrectPageData()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/lotteryresults?pageNumber=1&pageSize=5");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultsPageDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.PageSize);
            Assert.Equal(1, result.Data.PageNumber);
            Assert.True(result.Data.Results.Count <= 5);
            Assert.True(result.Data.TotalCount > 0);
        }

        [Fact]
        public async Task GetLotteryResults_WithDateFilter_ReturnsFilteredResults()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

            // Act
            var response = await _client.GetAsync($"/api/v1/lotteryresults?fromDate={fromDate}&toDate={toDate}");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultsPageDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.All(result.Data.Results, r => 
            {
                Assert.True(r.ResultDate >= DateTime.Parse(fromDate));
                Assert.True(r.ResultDate <= DateTime.Parse(toDate));
            });
        }

        [Fact]
        public async Task GetLotteryResults_WithSorting_ReturnsSortedResults()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/lotteryresults?sortBy=prizeValue&sortDirection=desc");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultsPageDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            
            // Verify sorting (descending by prize value)
            var results = result.Data.Results.ToList();
            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.True(results[i].PrizeValue >= results[i + 1].PrizeValue);
            }
        }

        [Fact]
        public async Task FullLotteryResultWorkflow_FromCreationToClaim_WorksCorrectly()
        {
            // This test simulates the complete workflow of a lottery result

            // 1. Get lottery results
            var getResultsResponse = await _client.GetAsync("/api/v1/lotteryresults");
            var getResultsContent = await getResultsResponse.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<ApiResponse<LotteryResultsPageDto>>(getResultsContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.True(results!.Success);
            Assert.NotNull(results.Data);
            Assert.True(results.Data.Results.Count > 0);

            // 2. Get specific result
            var result = results.Data.Results.First(r => !r.IsClaimed);
            var getResultResponse = await _client.GetAsync($"/api/v1/lotteryresults/{result.Id}");
            var getResultContent = await getResultResponse.Content.ReadAsStringAsync();
            var specificResult = JsonSerializer.Deserialize<ApiResponse<LotteryResultDto>>(getResultContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.True(specificResult!.Success);
            Assert.NotNull(specificResult.Data);
            Assert.Equal(result.Id, specificResult.Data.Id);

            // 3. Validate QR code
            var validateResponse = await _client.PostAsJsonAsync("/api/v1/lotteryresults/validate-qr", specificResult.Data.QRCodeData);
            var validateContent = await validateResponse.Content.ReadAsStringAsync();
            var validation = JsonSerializer.Deserialize<ApiResponse<QRCodeValidationDto>>(validateContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.True(validation!.Success);
            Assert.NotNull(validation.Data);
            Assert.True(validation.Data.IsValid);
            Assert.True(validation.Data.IsWinner);

            // 4. Claim prize
            var claimRequest = new ClaimPrizeRequest
            {
                ResultId = specificResult.Data.Id,
                ClaimNotes = "Integration test claim"
            };

            var claimResponse = await _client.PostAsJsonAsync("/api/v1/lotteryresults/claim", claimRequest);
            var claimContent = await claimResponse.Content.ReadAsStringAsync();
            var claimResult = JsonSerializer.Deserialize<ApiResponse<LotteryResultDto>>(claimContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.True(claimResult!.Success);
            Assert.NotNull(claimResult.Data);
            Assert.True(claimResult.Data.IsClaimed);

            // 5. If it's not first place, create delivery
            if (claimResult.Data!.PrizePosition > 1)
            {
                var deliveryRequest = new CreatePrizeDeliveryRequest
                {
                    LotteryResultId = claimResult.Data.Id,
                    RecipientName = "Integration Test Recipient",
                    AddressLine1 = "123 Integration Test Street",
                    City = "Test City",
                    State = "Test State",
                    PostalCode = "12345",
                    Country = "Test Country",
                    DeliveryMethod = "Standard"
                };

                var deliveryResponse = await _client.PostAsJsonAsync("/api/v1/lotteryresults/delivery", deliveryRequest);
                var deliveryContent = await deliveryResponse.Content.ReadAsStringAsync();
                var deliveryResult = JsonSerializer.Deserialize<ApiResponse<PrizeDeliveryDto>>(deliveryContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Assert.True(deliveryResult!.Success);
                Assert.NotNull(deliveryResult.Data);
                Assert.Equal(deliveryRequest.RecipientName, deliveryResult.Data.RecipientName);
            }
        }

        [Fact]
        public async Task GetLotteryResults_WithMultipleFilters_ReturnsCorrectResults()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/lotteryresults?prizePosition=1&isClaimed=true&pageSize=10");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultsPageDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.All(result.Data.Results, r => 
            {
                Assert.Equal(1, r.PrizePosition);
                Assert.True(r.IsClaimed);
            });
        }

        [Fact]
        public async Task GetLotteryResults_WithInvalidFilters_StillReturnsResults()
        {
            // Act - using invalid filter values should not break the API
            var response = await _client.GetAsync("/api/v1/lotteryresults?prizePosition=999&pageNumber=-1&pageSize=0");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LotteryResultsPageDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - API should handle invalid filters gracefully
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.True(result.Success);
            // Should return empty results or default behavior, not crash
        }

        public void Dispose()
        {
            _context.Dispose();
            _scope.Dispose();
            _client.Dispose();
        }
    }
}



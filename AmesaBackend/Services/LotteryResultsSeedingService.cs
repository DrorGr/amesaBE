using AmesaBackend.Data;
using AmesaBackend.Models;
using AmesaBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Services
{
    public static class LotteryResultsSeedingService
    {
        public static async Task SeedLotteryResultsAsync(AmesaDbContext context, IQRCodeService qrCodeService)
        {
            // Check if we already have lottery results
            if (await context.LotteryResults.AnyAsync())
            {
                return; // Already seeded
            }

            try
            {
                // Get some houses and users to create results for
                var houses = await context.Houses.Take(3).ToListAsync();
                var users = await context.Users.Take(5).ToListAsync();

                if (!houses.Any() || !users.Any())
                {
                    return; // Need houses and users first
                }

                var lotteryResults = new List<LotteryResult>();
                var random = new Random();

                foreach (var house in houses)
                {
                    // Create 3 prize positions for each house (1st, 2nd, 3rd)
                    for (int position = 1; position <= 3; position++)
                    {
                        var winnerUser = users[random.Next(users.Count)];
                        var ticketNumber = $"{house.Id.ToString()[..8]}-{random.Next(1000, 9999)}";
                        
                        // Generate QR code data
                        var lotteryResultId = Guid.NewGuid();
                        var qrCodeData = await qrCodeService.GenerateQRCodeDataAsync(
                            lotteryResultId, 
                            ticketNumber, 
                            position
                        );

                        var prizeValue = position switch
                        {
                            1 => house.Price, // 1st place gets the house
                            2 => house.Price * 0.1m, // 2nd place gets 10% of house value
                            3 => house.Price * 0.05m, // 3rd place gets 5% of house value
                            _ => 0
                        };

                        var prizeType = position switch
                        {
                            1 => "House",
                            2 => "Cash",
                            3 => "Cash",
                            _ => "Cash"
                        };

                        var prizeDescription = position switch
                        {
                            1 => $"Winner of {house.Title} - {house.Address}",
                            2 => $"Second place prize: ${prizeValue:N0} cash",
                            3 => $"Third place prize: ${prizeValue:N0} cash",
                            _ => "Prize"
                        };

                        var lotteryResult = new LotteryResult
                        {
                            Id = lotteryResultId,
                            LotteryId = house.Id,
                            DrawId = Guid.NewGuid(), // In real implementation, this would be linked to actual draws
                            WinnerTicketNumber = ticketNumber,
                            WinnerUserId = winnerUser.Id,
                            PrizePosition = position,
                            PrizeType = prizeType,
                            PrizeValue = prizeValue,
                            PrizeDescription = prizeDescription,
                            QRCodeData = qrCodeData,
                            QRCodeImageUrl = qrCodeService.GenerateQRCodeImageUrl(qrCodeData),
                            IsVerified = true,
                            IsClaimed = random.NextDouble() > 0.5, // Randomly claim some prizes
                            ClaimedAt = random.NextDouble() > 0.5 ? DateTime.UtcNow.AddDays(-random.Next(1, 30)) : null,
                            ResultDate = DateTime.UtcNow.AddDays(-random.Next(1, 90)), // Results from last 90 days
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 90)),
                            UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 90))
                        };

                        lotteryResults.Add(lotteryResult);

                        // Add history entry
                        var historyEntry = new LotteryResultHistory
                        {
                            Id = Guid.NewGuid(),
                            LotteryResultId = lotteryResult.Id,
                            Action = "Created",
                            Details = $"Lottery result created for {prizeDescription}",
                            PerformedBy = "System",
                            Timestamp = lotteryResult.CreatedAt,
                            IpAddress = "127.0.0.1",
                            UserAgent = "AmesaBackend/1.0"
                        };

                        context.LotteryResultHistory.Add(historyEntry);

                        // If claimed, add claim history
                        if (lotteryResult.IsClaimed && lotteryResult.ClaimedAt.HasValue)
                        {
                            var claimHistoryEntry = new LotteryResultHistory
                            {
                                Id = Guid.NewGuid(),
                                LotteryResultId = lotteryResult.Id,
                                Action = "Claimed",
                                Details = $"Prize claimed by winner",
                                PerformedBy = winnerUser.Email,
                                Timestamp = lotteryResult.ClaimedAt.Value,
                                IpAddress = "127.0.0.1",
                                UserAgent = "AmesaFrontend/1.0"
                            };

                            context.LotteryResultHistory.Add(claimHistoryEntry);
                        }
                    }
                }

                // Add lottery results to context
                await context.LotteryResults.AddRangeAsync(lotteryResults);
                await context.SaveChangesAsync();

                // Create some prize deliveries for 2nd and 3rd place winners
                var deliveries = new List<PrizeDelivery>();
                foreach (var result in lotteryResults.Where(r => r.PrizePosition > 1 && r.IsClaimed))
                {
                    var delivery = new PrizeDelivery
                    {
                        Id = Guid.NewGuid(),
                        LotteryResultId = result.Id,
                        WinnerUserId = result.WinnerUserId,
                        RecipientName = $"{users.First(u => u.Id == result.WinnerUserId).FirstName} {users.First(u => u.Id == result.WinnerUserId).LastName}",
                        AddressLine1 = $"{random.Next(100, 9999)} Main Street",
                        City = "Sample City",
                        State = "Sample State",
                        PostalCode = $"{random.Next(10000, 99999)}",
                        Country = "United States",
                        Phone = $"+1-555-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                        Email = users.First(u => u.Id == result.WinnerUserId).Email,
                        DeliveryMethod = "Standard",
                        DeliveryStatus = random.NextDouble() > 0.3 ? "Delivered" : "Pending",
                        EstimatedDeliveryDate = DateTime.UtcNow.AddDays(random.Next(1, 14)),
                        ActualDeliveryDate = random.NextDouble() > 0.3 ? DateTime.UtcNow.AddDays(-random.Next(1, 7)) : null,
                        ShippingCost = random.Next(10, 50),
                        DeliveryNotes = "Standard shipping via courier service",
                        CreatedAt = result.ClaimedAt ?? DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 7))
                    };

                    deliveries.Add(delivery);
                }

                await context.PrizeDeliveries.AddRangeAsync(deliveries);
                await context.SaveChangesAsync();

                // Create some scratch card results
                var scratchCards = new List<ScratchCardResult>();
                var scratchCardTypes = new[] { "Bronze", "Silver", "Gold", "Platinum" };

                for (int i = 0; i < 20; i++)
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

                await context.ScratchCardResults.AddRangeAsync(scratchCards);
                await context.SaveChangesAsync();

                Console.WriteLine($"✅ Seeded {lotteryResults.Count} lottery results, {deliveries.Count} prize deliveries, and {scratchCards.Count} scratch cards");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding lottery results: {ex.Message}");
                throw;
            }
        }
    }
}
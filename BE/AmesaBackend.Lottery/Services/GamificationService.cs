using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AmesaBackend.Lottery.Services;

public class GamificationService : IGamificationService
{
    private readonly LotteryDbContext _context;
    private readonly ILogger<GamificationService> _logger;

    public GamificationService(
        LotteryDbContext context,
        ILogger<GamificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<object> GetUserGamificationDataAsync(Guid userId)
    {
        try
        {
            // Get user's ticket statistics
            var totalTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId)
                .CountAsync();

            var activeTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId && t.Status == "Active")
                .CountAsync();

            var wonTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId && t.Status == "Won")
                .CountAsync();

            // Return gamification data
            return new
            {
                totalTickets = totalTickets,
                activeTickets = activeTickets,
                wonTickets = wonTickets,
                level = CalculateLevel(totalTickets),
                points = totalTickets * 10, // Simple points calculation
                achievements = new List<object>() // Can be expanded later
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gamification data for user {UserId}", userId);
            throw;
        }
    }

    private int CalculateLevel(int totalTickets)
    {
        // Simple level calculation: 1 level per 10 tickets
        return Math.Max(1, totalTickets / 10 + 1);
    }
}


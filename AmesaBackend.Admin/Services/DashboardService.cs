using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Payment.Data;
using AmesaBackend.Admin.Models;

namespace AmesaBackend.Admin.Services
{
    public interface IDashboardService
    {
        Task<DashboardStats> GetDashboardStatsAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly AuthDbContext _authContext;
        private readonly LotteryDbContext _lotteryContext;
        private readonly PaymentDbContext _paymentContext;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            AuthDbContext authContext,
            LotteryDbContext lotteryContext,
            PaymentDbContext paymentContext,
            ILogger<DashboardService> logger)
        {
            _authContext = authContext;
            _lotteryContext = lotteryContext;
            _paymentContext = paymentContext;
            _logger = logger;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var stats = new DashboardStats();

                // User statistics
                stats.TotalUsers = await _authContext.Users.CountAsync();
                stats.ActiveUsers = await _authContext.Users
                    .Where(u => u.Status == AmesaBackend.Auth.Models.UserStatus.Active)
                    .CountAsync();
                stats.NewUsersToday = await _authContext.Users
                    .Where(u => u.CreatedAt >= DateTime.UtcNow.Date)
                    .CountAsync();
                stats.NewUsersThisWeek = await _authContext.Users
                    .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync();

                // House statistics
                stats.TotalHouses = await _lotteryContext.Houses.CountAsync();
                stats.ActiveHouses = await _lotteryContext.Houses
                    .Where(h => h.Status == "Active" || h.Status == "Upcoming")
                    .CountAsync();
                stats.PendingHouses = await _lotteryContext.Houses
                    .Where(h => h.Status == "Pending")
                    .CountAsync();

                // Ticket statistics
                stats.TotalTickets = await _lotteryContext.LotteryTickets.CountAsync();
                stats.SoldTicketsToday = await _lotteryContext.LotteryTickets
                    .Where(t => t.PurchaseDate >= DateTime.UtcNow.Date && t.Status == "Active")
                    .CountAsync();
                stats.SoldTicketsThisWeek = await _lotteryContext.LotteryTickets
                    .Where(t => t.PurchaseDate >= DateTime.UtcNow.AddDays(-7) && t.Status == "Active")
                    .CountAsync();

                // Payment statistics
                var transactions = _paymentContext.Transactions;
                stats.TotalRevenue = await transactions
                    .Where(t => t.Status == "Completed")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;
                stats.RevenueToday = await transactions
                    .Where(t => t.Status == "Completed" && t.CreatedAt >= DateTime.UtcNow.Date)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;
                stats.RevenueThisWeek = await transactions
                    .Where(t => t.Status == "Completed" && t.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;
                stats.TotalTransactions = await transactions.CountAsync();
                stats.PendingTransactions = await transactions
                    .Where(t => t.Status == "Pending")
                    .CountAsync();

                // Draw statistics
                stats.TotalDraws = await _lotteryContext.LotteryDraws.CountAsync();
                stats.CompletedDraws = await _lotteryContext.LotteryDraws
                    .Where(d => d.DrawStatus == "Completed")
                    .CountAsync();
                stats.PendingDraws = await _lotteryContext.LotteryDraws
                    .Where(d => d.DrawStatus == "Pending")
                    .CountAsync();

                // Reservation statistics
                stats.ActiveReservations = await _lotteryContext.TicketReservations
                    .Where(r => r.Status == "Pending" && r.ExpiresAt > DateTime.UtcNow)
                    .CountAsync();
                stats.ExpiredReservations = await _lotteryContext.TicketReservations
                    .Where(r => r.Status == "Pending" && r.ExpiresAt <= DateTime.UtcNow)
                    .CountAsync();

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                return new DashboardStats(); // Return empty stats on error
            }
        }
    }
}


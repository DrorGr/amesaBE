namespace AmesaBackend.Admin.Models
{
    public class DashboardStats
    {
        // User statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }

        // House statistics
        public int TotalHouses { get; set; }
        public int ActiveHouses { get; set; }
        public int PendingHouses { get; set; }

        // Ticket statistics
        public int TotalTickets { get; set; }
        public int SoldTicketsToday { get; set; }
        public int SoldTicketsThisWeek { get; set; }

        // Payment statistics
        public decimal TotalRevenue { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public int TotalTransactions { get; set; }
        public int PendingTransactions { get; set; }

        // Draw statistics
        public int TotalDraws { get; set; }
        public int CompletedDraws { get; set; }
        public int PendingDraws { get; set; }

        // Reservation statistics
        public int ActiveReservations { get; set; }
        public int ExpiredReservations { get; set; }
    }
}


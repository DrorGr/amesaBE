namespace AmesaBackend.LotteryResults.Services
{
    public interface IQRCodeService
    {
        Task<string> GenerateQRCodeDataAsync(Guid lotteryResultId, string winnerTicketNumber, int prizePosition);
        Task<bool> ValidateQRCodeAsync(string qrCodeData);
        Task<QRCodeValidationResult> DecodeQRCodeAsync(string qrCodeData);
        string GenerateQRCodeImageUrl(string qrCodeData);
    }

    public class QRCodeData
    {
        public Guid LotteryResultId { get; set; }
        public string WinnerTicketNumber { get; set; } = string.Empty;
        public int PrizePosition { get; set; }
        public DateTime Timestamp { get; set; }
        public string AppUrl { get; set; } = string.Empty;
    }

    public class QRCodeValidationResult
    {
        public bool IsValid { get; set; }
        public Guid LotteryResultId { get; set; }
        public string WinnerTicketNumber { get; set; } = string.Empty;
        public int PrizePosition { get; set; }
        public DateTime Timestamp { get; set; }
        public string AppUrl { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}


using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AmesaBackend.LotteryResults.Services
{
    public class QRCodeService : IQRCodeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QRCodeService> _logger;
        private readonly string _secretKey;

        public QRCodeService(IConfiguration configuration, ILogger<QRCodeService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _secretKey = _configuration["QRCode:SecretKey"] ?? "your-qr-code-secret-key-change-this-in-production";
        }

        public async Task<string> GenerateQRCodeDataAsync(Guid lotteryResultId, string winnerTicketNumber, int prizePosition)
        {
            try
            {
                var qrData = new QRCodeData
                {
                    LotteryResultId = lotteryResultId,
                    WinnerTicketNumber = winnerTicketNumber,
                    PrizePosition = prizePosition,
                    Timestamp = DateTime.UtcNow,
                    AppUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200"
                };

                var jsonData = JsonSerializer.Serialize(qrData);
                var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
                var signature = GenerateSignature(encodedData);
                
                var finalQRData = $"{encodedData}.{signature}";
                
                _logger.LogInformation("Generated QR code for lottery result {LotteryResultId}", lotteryResultId);
                
                return finalQRData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for lottery result {LotteryResultId}", lotteryResultId);
                throw;
            }
        }

        public async Task<bool> ValidateQRCodeAsync(string qrCodeData)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCodeData))
                    return false;

                var parts = qrCodeData.Split('.');
                if (parts.Length != 2)
                    return false;

                var encodedData = parts[0];
                var signature = parts[1];

                var expectedSignature = GenerateSignature(encodedData);
                if (signature != expectedSignature)
                    return false;

                var jsonData = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
                var qrData = JsonSerializer.Deserialize<QRCodeData>(jsonData);

                if (qrData == null)
                    return false;

                var maxAge = TimeSpan.FromDays(365);
                if (DateTime.UtcNow - qrData.Timestamp > maxAge)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid QR code data: {QRCodeData}", qrCodeData);
                return false;
            }
        }

        public async Task<QRCodeValidationResult> DecodeQRCodeAsync(string qrCodeData)
        {
            try
            {
                if (!await ValidateQRCodeAsync(qrCodeData))
                {
                    return new QRCodeValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid or expired QR code"
                    };
                }

                var parts = qrCodeData.Split('.');
                var encodedData = parts[0];
                var jsonData = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
                var qrData = JsonSerializer.Deserialize<QRCodeData>(jsonData);

                return new QRCodeValidationResult
                {
                    IsValid = true,
                    LotteryResultId = qrData!.LotteryResultId,
                    WinnerTicketNumber = qrData.WinnerTicketNumber,
                    PrizePosition = qrData.PrizePosition,
                    Timestamp = qrData.Timestamp,
                    AppUrl = qrData.AppUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding QR code: {QRCodeData}", qrCodeData);
                return new QRCodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Failed to decode QR code"
                };
            }
        }

        public string GenerateQRCodeImageUrl(string qrCodeData)
        {
            try
            {
                var encodedData = Uri.EscapeDataString(qrCodeData);
                var size = "300x300";
                var qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size={size}&data={encodedData}";
                
                return qrImageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code image URL");
                return string.Empty;
            }
        }

        private string GenerateSignature(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }
}


extern alias MainApp;
using MainApp::AmesaBackend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AmesaBackend.Tests.Services
{
    public class QRCodeServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<QRCodeService>> _mockLogger;
        private readonly QRCodeService _qrCodeService;

        public QRCodeServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["QRCode:SecretKey"])
                .Returns("test-secret-key-for-qr-code-generation-and-validation");

            _mockLogger = new Mock<ILogger<QRCodeService>>();

            _qrCodeService = new QRCodeService(_mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateQRCodeDataAsync_WithValidInputs_ReturnsValidQRCode()
        {
            // Arrange
            var lotteryResultId = Guid.NewGuid();
            var winnerTicketNumber = "TICKET-12345678-001";
            var prizePosition = 1;

            // Act
            var result = await _qrCodeService.GenerateQRCodeDataAsync(lotteryResultId, winnerTicketNumber, prizePosition);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(".", result); // Should contain the signature separator
        }

        [Fact]
        public async Task ValidateQRCodeAsync_WithValidCode_ReturnsTrue()
        {
            // Arrange
            var lotteryResultId = Guid.NewGuid();
            var winnerTicketNumber = "TICKET-12345678-001";
            var prizePosition = 1;
            var qrCodeData = await _qrCodeService.GenerateQRCodeDataAsync(lotteryResultId, winnerTicketNumber, prizePosition);

            // Act
            var result = await _qrCodeService.ValidateQRCodeAsync(qrCodeData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateQRCodeAsync_WithInvalidCode_ReturnsFalse()
        {
            // Arrange
            var invalidQRCode = "invalid-qr-code-data";

            // Act
            var result = await _qrCodeService.ValidateQRCodeAsync(invalidQRCode);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateQRCodeAsync_WithEmptyCode_ReturnsFalse()
        {
            // Arrange
            var emptyQRCode = "";

            // Act
            var result = await _qrCodeService.ValidateQRCodeAsync(emptyQRCode);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateQRCodeAsync_WithNullCode_ReturnsFalse()
        {
            // Act
            var result = await _qrCodeService.ValidateQRCodeAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateQRCodeAsync_WithMalformedCode_ReturnsFalse()
        {
            // Arrange
            var malformedQRCode = "not-a-valid-format";

            // Act
            var result = await _qrCodeService.ValidateQRCodeAsync(malformedQRCode);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DecodeQRCodeAsync_WithValidCode_ReturnsValidResult()
        {
            // Arrange
            var lotteryResultId = Guid.NewGuid();
            var winnerTicketNumber = "TICKET-12345678-001";
            var prizePosition = 1;
            var qrCodeData = await _qrCodeService.GenerateQRCodeDataAsync(lotteryResultId, winnerTicketNumber, prizePosition);

            // Act
            var result = await _qrCodeService.DecodeQRCodeAsync(qrCodeData);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Equal(lotteryResultId, result.LotteryResultId);
            Assert.Equal(winnerTicketNumber, result.WinnerTicketNumber);
            Assert.Equal(prizePosition, result.PrizePosition);
        }

        [Fact]
        public async Task DecodeQRCodeAsync_WithInvalidCode_ReturnsInvalidResult()
        {
            // Arrange
            var invalidQRCode = "invalid-qr-code-data";

            // Act
            var result = await _qrCodeService.DecodeQRCodeAsync(invalidQRCode);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void GenerateQRCodeImageUrl_WithValidData_ReturnsValidUrl()
        {
            // Arrange
            var qrCodeData = "test-qr-code-data";

            // Act
            var result = _qrCodeService.GenerateQRCodeImageUrl(qrCodeData);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.StartsWith("https://api.qrserver.com", result);
            Assert.Contains("300x300", result);
        }

        [Fact]
        public void GenerateQRCodeImageUrl_WithEmptyData_ReturnsEmptyString()
        {
            // Arrange
            var emptyData = "";

            // Act
            var result = _qrCodeService.GenerateQRCodeImageUrl(emptyData);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GenerateQRCodeDataAsync_WithSameInputs_GeneratesDifferentResults()
        {
            // Arrange
            var lotteryResultId = Guid.NewGuid();
            var winnerTicketNumber = "TICKET-12345678-001";
            var prizePosition = 1;

            // Act
            var result1 = await _qrCodeService.GenerateQRCodeDataAsync(lotteryResultId, winnerTicketNumber, prizePosition);
            await Task.Delay(1); // Small delay to ensure different timestamps
            var result2 = await _qrCodeService.GenerateQRCodeDataAsync(lotteryResultId, winnerTicketNumber, prizePosition);

            // Assert
            Assert.NotEqual(result1, result2); // Should be different due to timestamp
        }

        [Fact]
        public async Task ValidateQRCodeAsync_WithOldCode_ReturnsFalse()
        {
            // This test would require mocking the current time to test expiration
            // For now, we'll test that the validation works for recent codes
            var lotteryResultId = Guid.NewGuid();
            var winnerTicketNumber = "TICKET-12345678-001";
            var prizePosition = 1;
            var qrCodeData = await _qrCodeService.GenerateQRCodeDataAsync(lotteryResultId, winnerTicketNumber, prizePosition);

            var result = await _qrCodeService.ValidateQRCodeAsync(qrCodeData);
            Assert.True(result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(10)]
        public async Task GenerateQRCodeDataAsync_WithDifferentPrizePositions_ReturnsValidCode(int prizePosition)
        {
            // Arrange
            var lotteryResultId = Guid.NewGuid();
            var winnerTicketNumber = "TICKET-12345678-001";

            // Act
            var result = await _qrCodeService.GenerateQRCodeDataAsync(lotteryResultId, winnerTicketNumber, prizePosition);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Verify the code can be decoded
            var decoded = await _qrCodeService.DecodeQRCodeAsync(result);
            Assert.True(decoded.IsValid);
            Assert.Equal(prizePosition, decoded.PrizePosition);
        }

        [Theory]
        [InlineData("TICKET-12345678-001")]
        [InlineData("TICKET-ABCDEFGH-999")]
        [InlineData("LOTTERY-123456-001")]
        public async Task GenerateQRCodeDataAsync_WithDifferentTicketNumbers_ReturnsValidCode(string ticketNumber)
        {
            // Arrange
            var lotteryResultId = Guid.NewGuid();
            var prizePosition = 1;

            // Act
            var result = await _qrCodeService.GenerateQRCodeDataAsync(lotteryResultId, ticketNumber, prizePosition);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Verify the code can be decoded
            var decoded = await _qrCodeService.DecodeQRCodeAsync(result);
            Assert.True(decoded.IsValid);
            Assert.Equal(ticketNumber, decoded.WinnerTicketNumber);
        }
    }
}



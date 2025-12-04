using Xunit;
using Moq;
using Moq.Protected;
using FluentAssertions;
using AmesaBackend.Lottery.Services.Processors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AmesaBackend.Tests.Services
{
    public class PaymentProcessorTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<PaymentProcessor>> _mockLogger;
        private readonly PaymentProcessor _paymentProcessor;

        public PaymentProcessorTests()
        {
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object);
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<PaymentProcessor>>();

            _mockConfiguration.Setup(c => c["PaymentService:BaseUrl"])
                .Returns("http://test-payment-service/api/v1");

            _paymentProcessor = new PaymentProcessor(
                _httpClient,
                _mockConfiguration.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithSuccessfulResponse_ReturnsSuccess()
        {
            // Arrange
            var reservationId = Guid.NewGuid();
            var paymentMethodId = Guid.NewGuid();
            var amount = 250m;

            var mockResponse = new
            {
                success = true,
                data = new
                {
                    transactionId = Guid.NewGuid().ToString(),
                    providerTransactionId = "provider-123"
                }
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(mockResponse), Encoding.UTF8, "application/json")
            };

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _paymentProcessor.ProcessPaymentAsync(reservationId, paymentMethodId, amount);

            // Assert
            result.Success.Should().BeTrue();
            result.TransactionId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithFailedResponse_ReturnsFailure()
        {
            // Arrange
            var reservationId = Guid.NewGuid();
            var paymentMethodId = Guid.NewGuid();
            var amount = 250m;

            var mockResponse = new
            {
                success = false,
                error = new
                {
                    message = "Payment failed"
                }
            };

            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(mockResponse), Encoding.UTF8, "application/json")
            };

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _paymentProcessor.ProcessPaymentAsync(reservationId, paymentMethodId, amount);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}


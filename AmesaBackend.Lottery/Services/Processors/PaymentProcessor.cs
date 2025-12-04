using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AmesaBackend.Lottery.Services.Processors
{
    public interface IPaymentProcessor
    {
        Task<PaymentProcessResult> ProcessPaymentAsync(Guid reservationId, Guid paymentMethodId, decimal amount, CancellationToken cancellationToken = default);
        Task<bool> RefundPaymentAsync(Guid transactionId, decimal amount, CancellationToken cancellationToken = default);
    }

    public class PaymentProcessor : IPaymentProcessor
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentProcessor> _logger;

        public PaymentProcessor(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PaymentProcessor> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PaymentProcessResult> ProcessPaymentAsync(
            Guid reservationId, 
            Guid paymentMethodId, 
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var paymentServiceUrl = _configuration["PaymentService:BaseUrl"] 
                    ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";

                var request = new
                {
                    PaymentMethodId = paymentMethodId,
                    Amount = amount,
                    Currency = "USD",
                    Description = $"Lottery ticket reservation {reservationId}",
                    ReferenceId = reservationId.ToString(),
                    IdempotencyKey = reservationId.ToString()
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{paymentServiceUrl}/payments/process",
                    request,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PaymentApiResponse>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        cancellationToken);

                    if (result?.Success == true && result.Data != null)
                    {
                        // Safely parse TransactionId with validation
                        if (string.IsNullOrEmpty(result.Data.TransactionId) || 
                            !Guid.TryParse(result.Data.TransactionId, out var transactionId))
                        {
                            _logger.LogError("Invalid TransactionId in payment response: {TransactionId}", 
                                result.Data.TransactionId);
                            return new PaymentProcessResult
                            {
                                Success = false,
                                ErrorMessage = "Invalid transaction ID received from payment service"
                            };
                        }

                        return new PaymentProcessResult
                        {
                            Success = true,
                            TransactionId = transactionId,
                            ProviderTransactionId = result.Data.ProviderTransactionId
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Payment failed for reservation {ReservationId}: {Error}",
                    reservationId, errorContent);

                return new PaymentProcessResult
                {
                    Success = false,
                    ErrorMessage = $"Payment failed: {response.StatusCode}"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error processing payment for reservation {ReservationId}",
                    reservationId);
                return new PaymentProcessResult
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for reservation {ReservationId}",
                    reservationId);
                return new PaymentProcessResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> RefundPaymentAsync(
            Guid transactionId, 
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var paymentServiceUrl = _configuration["PaymentService:BaseUrl"] 
                    ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";

                var request = new
                {
                    TransactionId = transactionId,
                    Amount = amount,
                    Reason = "Ticket creation failed"
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{paymentServiceUrl}/payments/refund",
                    request,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PaymentApiResponse>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        cancellationToken);

                    if (result?.Success == true)
                    {
                        _logger.LogInformation("Successfully refunded transaction {TransactionId}, amount {Amount}",
                            transactionId, amount);
                        return true;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Refund failed for transaction {TransactionId}: {Error}",
                    transactionId, errorContent);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error refunding transaction {TransactionId}", transactionId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding transaction {TransactionId}", transactionId);
                return false;
            }
        }
    }

    public class PaymentProcessResult
    {
        public bool Success { get; set; }
        public Guid TransactionId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PaymentApiResponse
    {
        public bool Success { get; set; }
        public PaymentResponseData? Data { get; set; }
    }

    public class PaymentResponseData
    {
        public string? TransactionId { get; set; }
        public string? ProviderTransactionId { get; set; }
    }
}




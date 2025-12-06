using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Models;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Shared.Contracts;
using System.Text.Json;

namespace AmesaBackend.Payment.Services.ProductHandlers;

public class LotteryTicketProductHandler : IProductHandler
{
    public string HandlesType => "lottery_ticket";

    private readonly ILogger<LotteryTicketProductHandler> _logger;
    private readonly IHttpRequest _httpRequest;

    public LotteryTicketProductHandler(
        ILogger<LotteryTicketProductHandler> logger,
        IHttpRequest httpRequest)
    {
        _logger = logger;
        _httpRequest = httpRequest;
    }

    public async Task<ProductValidationResult> ValidatePurchaseAsync(
        Guid productId, 
        int quantity, 
        Guid userId, 
        PaymentDbContext context)
    {
        // Get product link to house
        var productLink = await context.ProductLinks
            .FirstOrDefaultAsync(pl => pl.ProductId == productId && pl.LinkedEntityType == "house");

        if (productLink == null)
        {
            return new ProductValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Product not linked to a house" }
            };
        }

        var houseId = productLink.LinkedEntityId;

        // Validate with Lottery service via HTTP
        try
        {
            var lotteryServiceUrl = Environment.GetEnvironmentVariable("LOTTERY_SERVICE_URL");
            if (string.IsNullOrEmpty(lotteryServiceUrl))
            {
                _logger.LogError("LOTTERY_SERVICE_URL environment variable is required but not configured");
                throw new InvalidOperationException(
                    "LOTTERY_SERVICE_URL environment variable is required. " +
                    "Please configure it in ECS task definition.");
            }
            
            var validationRequest = new
            {
                HouseId = houseId,
                Quantity = quantity,
                UserId = userId
            };

            var response = await _httpRequest.PostRequest<AmesaBackend.Shared.Contracts.ApiResponse<ValidateTicketsResponse>>(
                $"{lotteryServiceUrl}/api/v1/houses/{houseId}/tickets/validate",
                validationRequest);

            if (response == null || response.IsError)
            {
                var errorMessages = new List<string>();
                
                // Extract specific validation errors if available
                if (response?.ResponseException != null)
                {
                    errorMessages.Add(response.ResponseException.ExceptionMessage ?? "Validation failed");
                    if (!string.IsNullOrEmpty(response.ResponseException.ReferenceErrorCode))
                    {
                        errorMessages.Add($"Error Code: {response.ResponseException.ReferenceErrorCode}");
                    }
                }
                else if (response?.Data != null && !response.Data.IsValid)
                {
                    // Response has validation result with specific errors
                    errorMessages.AddRange(response.Data.Errors);
                }
                
                if (errorMessages.Count == 0)
                {
                    errorMessages.Add("Failed to validate ticket purchase with lottery service");
                }
                
                return new ProductValidationResult
                {
                    IsValid = false,
                    Errors = errorMessages
                };
            }

            // Check validation result data
            if (response.Data != null && !response.Data.IsValid)
            {
                return new ProductValidationResult
                {
                    IsValid = false,
                    Errors = response.Data.Errors
                };
            }

            return new ProductValidationResult
            {
                IsValid = true,
                Errors = new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating lottery ticket purchase for product {ProductId}", productId);
            return new ProductValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Error validating purchase" }
            };
        }
    }

    public async Task<ProcessPurchaseResult> ProcessPurchaseAsync(
        Guid transactionId,
        Guid productId,
        int quantity,
        Guid userId,
        PaymentDbContext context,
        IEventPublisher eventPublisher)
    {
        // Get product link to house
        var productLink = await context.ProductLinks
            .FirstOrDefaultAsync(pl => pl.ProductId == productId && pl.LinkedEntityType == "house");

        if (productLink == null)
        {
            throw new InvalidOperationException("Product not linked to a house");
        }

        var houseId = productLink.LinkedEntityId;

        // Call Lottery service to create tickets via HTTP
        try
        {
            var lotteryServiceUrl = Environment.GetEnvironmentVariable("LOTTERY_SERVICE_URL");
            if (string.IsNullOrEmpty(lotteryServiceUrl))
            {
                _logger.LogError("LOTTERY_SERVICE_URL environment variable is required but not configured");
                throw new InvalidOperationException(
                    "LOTTERY_SERVICE_URL environment variable is required. " +
                    "Please configure it in ECS task definition.");
            }
            
            var createTicketsRequest = new
            {
                HouseId = houseId,
                Quantity = quantity,
                PaymentId = transactionId,
                UserId = userId
            };

            var response = await _httpRequest.PostRequest<AmesaBackend.Shared.Contracts.ApiResponse<CreateTicketsResponse>>(
                $"{lotteryServiceUrl}/api/v1/houses/{houseId}/tickets/create-from-payment",
                createTicketsRequest);

            if (response == null || response.IsError || response.Data == null)
            {
                throw new InvalidOperationException("Failed to create lottery tickets");
            }

            // Publish TicketPurchasedEvent for real-time updates and notifications
            try
            {
                await eventPublisher.PublishAsync(new TicketPurchasedEvent
                {
                    UserId = userId,
                    HouseId = houseId,
                    TicketCount = response.Data.TicketsPurchased,
                    TicketNumbers = response.Data.TicketNumbers ?? new List<string>()
                });
                
                _logger.LogInformation(
                    "Published TicketPurchasedEvent for user {UserId}, house {HouseId}, tickets {TicketCount}",
                    userId, houseId, response.Data.TicketsPurchased);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to publish TicketPurchasedEvent for transaction {TransactionId} (non-critical)",
                    transactionId);
                // Don't fail the purchase if event publishing fails
            }

            return new ProcessPurchaseResult
            {
                Success = true,
                LinkedEntityId = houseId,
                LinkedEntityType = "house",
                Metadata = new Dictionary<string, object>
                {
                    ["TicketNumbers"] = response.Data?.TicketNumbers ?? new List<string>(),
                    ["TicketsPurchased"] = response.Data?.TicketsPurchased ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lottery tickets for transaction {TransactionId}", transactionId);
            
            // Attempt to refund payment if ticket creation failed
            try
            {
                _logger.LogWarning("Ticket creation failed for transaction {TransactionId}. Attempting refund.", transactionId);
                
                // Note: Refund would require IPaymentRefundService or similar
                // For now, log the refund requirement - actual refund should be handled by payment service
                // or a separate refund service/endpoint
                
                // TODO: Implement refund call once refund endpoint is available
                // This should call a refund service that calls the payment service's refund endpoint
                _logger.LogWarning("Refund required for transaction {TransactionId} but refund endpoint not yet implemented", transactionId);
            }
            catch (Exception refundEx)
            {
                _logger.LogError(refundEx, "Failed to initiate refund for transaction {TransactionId} after ticket creation failure", transactionId);
            }
            
            throw;
        }
    }

    public async Task<decimal> CalculatePriceAsync(Guid productId, int quantity, Guid? userId, PaymentDbContext context)
    {
        var product = await context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        return product.BasePrice * quantity;
    }

    private class CreateTicketsResponse
    {
        public List<string> TicketNumbers { get; set; } = new();
        public int TicketsPurchased { get; set; }
    }

    private class ValidateTicketsResponse
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public decimal TotalCost { get; set; }
        public bool CanEnter { get; set; }
    }
}


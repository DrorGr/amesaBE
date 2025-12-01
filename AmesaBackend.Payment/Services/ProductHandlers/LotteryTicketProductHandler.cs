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
            var lotteryServiceUrl = Environment.GetEnvironmentVariable("LOTTERY_SERVICE_URL") 
                ?? "http://localhost:5001";
            
            var validationRequest = new
            {
                HouseId = houseId,
                Quantity = quantity,
                UserId = userId
            };

            var response = await _httpRequest.PostRequest<AmesaBackend.Shared.Contracts.ApiResponse<object>>(
                $"{lotteryServiceUrl}/api/v1/houses/{houseId}/tickets/validate",
                validationRequest);

            if (response == null || response.IsError)
            {
                return new ProductValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "Failed to validate ticket purchase with lottery service" }
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
            var lotteryServiceUrl = Environment.GetEnvironmentVariable("LOTTERY_SERVICE_URL") 
                ?? "http://localhost:5001";
            
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
}


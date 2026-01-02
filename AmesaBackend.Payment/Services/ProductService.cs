using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Models;
using AmesaBackend.Payment.Services.Interfaces;
using System.Text.Json;

namespace AmesaBackend.Payment.Services;

public class ProductService : IProductService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(PaymentDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductDto> GetProductAsync(Guid productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null);

        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        return MapToProductDto(product);
    }

    public async Task<ProductDto> GetProductByCodeAsync(string code)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Code == code && p.DeletedAt == null);

        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        return MapToProductDto(product);
    }

    public async Task<ProductDto?> GetProductByHouseIdAsync(Guid houseId)
    {
        ProductLink? productLink = null;
        try
        {
            // Fix: Check Product nullability before accessing DeletedAt
            productLink = await _context.ProductLinks
                .Include(pl => pl.Product)
                .FirstOrDefaultAsync(pl => pl.LinkedEntityType == "house" && 
                                            pl.LinkedEntityId == houseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database query exception in GetProductByHouseIdAsync for house {HouseId}", houseId);
            throw;
        }

        if (productLink == null || productLink.Product == null)
        {
            return null;
        }

        // Check if product is deleted
        if (productLink.Product.DeletedAt != null)
        {
            return null;
        }
        
        try
        {
            var result = MapToProductDto(productLink.Product);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MapToProductDto exception for product {ProductId}", productLink.Product.Id);
            throw;
        }
    }

    public async Task<List<ProductDto>> GetActiveProductsAsync(string? productType = null)
    {
        var query = _context.Products
            .Where(p => p.IsActive && p.Status == "active" && p.DeletedAt == null);

        if (!string.IsNullOrEmpty(productType))
        {
            query = query.Where(p => p.ProductType == productType);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToProductDto).ToList();
    }

    public async Task<bool> IsProductAvailableAsync(Guid productId, int quantity = 1, Guid? userId = null)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || !product.IsActive || product.Status != "active" || product.DeletedAt != null)
        {
            return false;
        }

        // Check availability dates
        if (product.AvailableFrom.HasValue && DateTime.UtcNow < product.AvailableFrom.Value)
        {
            return false;
        }

        if (product.AvailableUntil.HasValue && DateTime.UtcNow > product.AvailableUntil.Value)
        {
            return false;
        }

        // Check total quantity
        if (product.TotalQuantityAvailable.HasValue)
        {
            var remaining = product.TotalQuantityAvailable.Value - product.QuantitySold;
            if (quantity > remaining)
            {
                return false;
            }
        }

        // Check user quantity limit
        if (userId.HasValue && product.MaxQuantityPerUser.HasValue)
        {
            // Fix: Add Include to properly load Transaction navigation property
            var userPurchases = await _context.TransactionItems
                .Include(ti => ti.Transaction)
                .Where(ti => ti.ProductId == productId &&
                             ti.Transaction.UserId == userId.Value &&
                             ti.Transaction.Status == "Completed")
                .SumAsync(ti => ti.Quantity);

            if (userPurchases + quantity > product.MaxQuantityPerUser.Value)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<decimal> CalculatePriceAsync(Guid productId, int quantity = 1, Guid? userId = null)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        // Server-side price calculation - never trust client
        decimal totalPrice = product.BasePrice * quantity;

        // Apply pricing model logic if needed
        if (product.PricingModel == "tiered" && product.PricingMetadata != null)
        {
            // Example: Buy 10+ get 10% discount
            // This would be implemented based on PricingMetadata structure
        }

        return totalPrice;
    }

    public async Task<ProductValidationResult> ValidateProductPurchaseAsync(Guid productId, int quantity, Guid userId)
    {
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && p.Status == "active" && p.DeletedAt == null);

            if (product == null)
            {
                return new ProductValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "Product not found or not available" }
                };
            }

            var errors = new List<string>();

            // Validate quantity
            if (quantity <= 0)
            {
                errors.Add("Quantity must be greater than zero");
            }

            if (product.MaxQuantityPerUser.HasValue && quantity > product.MaxQuantityPerUser.Value)
            {
                errors.Add($"Maximum quantity per user is {product.MaxQuantityPerUser.Value}");
            }

            // Validate availability dates
            if (product.AvailableFrom.HasValue && DateTime.UtcNow < product.AvailableFrom.Value)
            {
                errors.Add("Product is not yet available");
            }

            if (product.AvailableUntil.HasValue && DateTime.UtcNow > product.AvailableUntil.Value)
            {
                errors.Add("Product is no longer available");
            }

            // Validate total quantity
            if (product.TotalQuantityAvailable.HasValue)
            {
                var remaining = product.TotalQuantityAvailable.Value - product.QuantitySold;
                if (quantity > remaining)
                {
                    errors.Add($"Only {remaining} items remaining");
                }
            }

            // Validate user quantity limit
            if (product.MaxQuantityPerUser.HasValue)
            {
                try
                {
                    // Fix: Add Include to properly load Transaction navigation property
                    var userPurchases = await _context.TransactionItems
                        .Include(ti => ti.Transaction)
                        .Where(ti => ti.ProductId == productId &&
                                     ti.Transaction.UserId == userId &&
                                     ti.Transaction.Status == "Completed")
                        .SumAsync(ti => ti.Quantity);

                    if (userPurchases + quantity > product.MaxQuantityPerUser.Value)
                    {
                        errors.Add($"You have already purchased {userPurchases} items. Maximum allowed is {product.MaxQuantityPerUser.Value}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating user purchase quantity for product {ProductId}", productId);
                    throw;
                }
            }

            var calculatedPrice = errors.Count == 0 ? await CalculatePriceAsync(productId, quantity, userId) : 0;

            return new ProductValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                CalculatedPrice = calculatedPrice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product purchase for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, Guid createdBy)
    {
        // Check if code already exists
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Code == request.Code && p.DeletedAt == null);

        if (existingProduct != null)
        {
            throw new InvalidOperationException("Product code already exists");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            ProductType = request.ProductType,
            Status = "active",
            BasePrice = request.BasePrice,
            Currency = request.Currency,
            PricingModel = request.PricingModel,
            PricingMetadata = request.PricingMetadata != null ? JsonSerializer.Serialize(request.PricingMetadata) : null,
            AvailableFrom = request.AvailableFrom,
            AvailableUntil = request.AvailableUntil,
            MaxQuantityPerUser = request.MaxQuantityPerUser,
            TotalQuantityAvailable = request.TotalQuantityAvailable,
            QuantitySold = 0,
            ProductMetadata = request.ProductMetadata != null ? JsonSerializer.Serialize(request.ProductMetadata) : null,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToProductDto(product);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductRequest request, Guid updatedBy)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null);

        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        if (request.Name != null)
            product.Name = request.Name;

        if (request.Description != null)
            product.Description = request.Description;

        if (request.Status != null)
            product.Status = request.Status;

        if (request.BasePrice.HasValue)
            product.BasePrice = request.BasePrice.Value;

        if (request.Currency != null)
            product.Currency = request.Currency;

        if (request.PricingModel != null)
            product.PricingModel = request.PricingModel;

        if (request.PricingMetadata != null)
            product.PricingMetadata = JsonSerializer.Serialize(request.PricingMetadata);

        if (request.AvailableFrom.HasValue)
            product.AvailableFrom = request.AvailableFrom;

        if (request.AvailableUntil.HasValue)
            product.AvailableUntil = request.AvailableUntil;

        if (request.MaxQuantityPerUser.HasValue)
            product.MaxQuantityPerUser = request.MaxQuantityPerUser;

        if (request.TotalQuantityAvailable.HasValue)
            product.TotalQuantityAvailable = request.TotalQuantityAvailable;

        if (request.IsActive.HasValue)
            product.IsActive = request.IsActive.Value;

        if (request.ProductMetadata != null)
            product.ProductMetadata = JsonSerializer.Serialize(request.ProductMetadata);

        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToProductDto(product);
    }

    public async Task DeactivateProductAsync(Guid productId, Guid updatedBy)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        product.IsActive = false;
        product.Status = "inactive";
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task ActivateProductAsync(Guid productId, Guid updatedBy)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        product.IsActive = true;
        product.Status = "active";
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task LinkProductAsync(Guid productId, string entityType, Guid entityId, Dictionary<string, object>? metadata = null)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        var existingLink = await _context.ProductLinks
            .FirstOrDefaultAsync(pl => pl.ProductId == productId &&
                                      pl.LinkedEntityType == entityType &&
                                      pl.LinkedEntityId == entityId);

        if (existingLink != null)
        {
            return; // Link already exists
        }

        var link = new ProductLink
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            LinkedEntityType = entityType,
            LinkedEntityId = entityId,
            LinkMetadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductLinks.Add(link);
        await _context.SaveChangesAsync();
    }

    public async Task UnlinkProductAsync(Guid productId, string entityType, Guid entityId)
    {
        var link = await _context.ProductLinks
            .FirstOrDefaultAsync(pl => pl.ProductId == productId &&
                                      pl.LinkedEntityType == entityType &&
                                      pl.LinkedEntityId == entityId);

        if (link != null)
        {
            _context.ProductLinks.Remove(link);
            await _context.SaveChangesAsync();
        }
    }

    private ProductDto MapToProductDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Description = product.Description,
            ProductType = product.ProductType,
            Status = product.Status,
            BasePrice = product.BasePrice,
            Currency = product.Currency,
            PricingModel = product.PricingModel,
            PricingMetadata = product.PricingMetadata != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(product.PricingMetadata) : null,
            AvailableFrom = product.AvailableFrom,
            AvailableUntil = product.AvailableUntil,
            MaxQuantityPerUser = product.MaxQuantityPerUser,
            TotalQuantityAvailable = product.TotalQuantityAvailable,
            QuantitySold = product.QuantitySold,
            ProductMetadata = product.ProductMetadata != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(product.ProductMetadata) : null,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}


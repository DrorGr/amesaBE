using AmesaBackend.Payment.DTOs;

namespace AmesaBackend.Payment.Services;

public interface IProductService
{
    Task<ProductDto> GetProductAsync(Guid productId);
    Task<ProductDto> GetProductByCodeAsync(string code);
    Task<ProductDto?> GetProductByHouseIdAsync(Guid houseId);
    Task<List<ProductDto>> GetActiveProductsAsync(string? productType = null);
    Task<bool> IsProductAvailableAsync(Guid productId, int quantity = 1, Guid? userId = null);
    Task<decimal> CalculatePriceAsync(Guid productId, int quantity = 1, Guid? userId = null);
    Task<ProductValidationResult> ValidateProductPurchaseAsync(Guid productId, int quantity, Guid userId);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, Guid createdBy);
    Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductRequest request, Guid updatedBy);
    Task DeactivateProductAsync(Guid productId, Guid updatedBy);
    Task ActivateProductAsync(Guid productId, Guid updatedBy);
    Task LinkProductAsync(Guid productId, string entityType, Guid entityId, Dictionary<string, object>? metadata = null);
    Task UnlinkProductAsync(Guid productId, string entityType, Guid entityId);
}


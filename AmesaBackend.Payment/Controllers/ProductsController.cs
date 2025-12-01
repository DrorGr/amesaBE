using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Helpers;

namespace AmesaBackend.Payment.Controllers;
// CI/CD trigger - ensure payment service rebuilds with ProductsController

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;
    private readonly IPaymentAuditService? _auditService;

    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger,
        IServiceProvider serviceProvider)
    {
        _productService = productService;
        _logger = logger;
        _auditService = serviceProvider.GetService<IPaymentAuditService>();
    }

    [HttpGet]
    [AllowAnonymous] // Public product listing
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProducts([FromQuery] string? type = null)
    {
        try
        {
            var products = await _productService.GetActiveProductsAsync(type);
            return Ok(new ApiResponse<List<ProductDto>> { Success = true, Data = products });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, new ApiResponse<List<ProductDto>> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "INTERNAL_ERROR", 
                    Message = "An error occurred retrieving products" 
                } 
            });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id)
    {
        try
        {
            var product = await _productService.GetProductAsync(id);
            return Ok(new ApiResponse<ProductDto> { Success = true, Data = product });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<ProductDto> { Success = false, Message = "Product not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", id);
            return StatusCode(500, new ApiResponse<ProductDto> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "INTERNAL_ERROR", 
                    Message = "An error occurred retrieving product" 
                } 
            });
        }
    }

    [HttpPost("{id}/validate")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ProductValidationResponse>>> ValidateProduct(
        Guid id, 
        [FromBody] ProductValidationRequest request)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<ProductValidationResponse>();
            }

            var result = await _productService.ValidateProductPurchaseAsync(id, request.Quantity, userId);
            
            return Ok(new ApiResponse<ProductValidationResponse>
            {
                Success = true,
                Data = new ProductValidationResponse
                {
                    IsValid = result.IsValid,
                    Errors = result.Errors,
                    CalculatedPrice = result.CalculatedPrice
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product {ProductId}", id);
            return StatusCode(500, new ApiResponse<ProductValidationResponse> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "VALIDATION_ERROR", 
                    Message = "An error occurred validating product purchase" 
                } 
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] // Admin only
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<ProductDto>();
            }

            var product = await _productService.CreateProductAsync(request, userId);
            
            // Audit log
            if (_auditService != null)
            {
                await _auditService.LogActionAsync(
                    userId, 
                    "product_created", 
                    "product", 
                    product.Id, 
                    null, 
                    null,
                    ControllerHelpers.GetIpAddress(HttpContext), 
                    ControllerHelpers.GetUserAgent(HttpContext), 
                    new Dictionary<string, object> { ["ProductCode"] = product.Code });
            }

            return Ok(new ApiResponse<ProductDto> { Success = true, Data = product });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ProductDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, new ApiResponse<ProductDto> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "INTERNAL_ERROR", 
                    Message = "An error occurred creating product" 
                } 
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(
        Guid id, 
        [FromBody] UpdateProductRequest request)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<ProductDto>();
            }

            var product = await _productService.UpdateProductAsync(id, request, userId);
            return Ok(new ApiResponse<ProductDto> { Success = true, Data = product });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<ProductDto> { Success = false, Message = "Product not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, new ApiResponse<ProductDto> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "INTERNAL_ERROR", 
                    Message = "An error occurred updating product" 
                } 
            });
        }
    }
}


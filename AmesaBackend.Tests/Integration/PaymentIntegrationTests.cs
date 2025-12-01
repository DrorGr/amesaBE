using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.Models;
using AmesaBackend.Payment.Services.ProductHandlers;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace AmesaBackend.Tests.Integration;

/// <summary>
/// Integration tests for Payment service.
/// Note: These tests use in-memory database and mocked external dependencies.
/// For full integration tests against the Payment microservice, run tests against the deployed service.
/// </summary>
public class PaymentIntegrationTests : IDisposable
{
    private readonly PaymentDbContext _dbContext;
    private readonly Mock<ILogger<ProductService>> _mockProductLogger;
    private readonly Mock<IProductHandlerRegistry> _mockHandlerRegistry;
    private readonly Mock<ILogger<LotteryTicketProductHandler>> _mockHandlerLogger;
    private readonly Mock<IHttpRequest> _mockHttpRequest;
    private readonly ProductService _productService;
    private readonly LotteryTicketProductHandler _lotteryHandler;

    public PaymentIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: $"PaymentIntegrationTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new PaymentDbContext(options);

        _mockProductLogger = new Mock<ILogger<ProductService>>();
        _mockHandlerRegistry = new Mock<IProductHandlerRegistry>();
        _mockHandlerLogger = new Mock<ILogger<LotteryTicketProductHandler>>();
        _mockHttpRequest = new Mock<IHttpRequest>();

        _productService = new ProductService(_dbContext, _mockProductLogger.Object, _mockHandlerRegistry.Object);
        _lotteryHandler = new LotteryTicketProductHandler(_mockHandlerLogger.Object, _mockHttpRequest.Object);
    }

    [Fact]
    public async Task ProductService_CreateProduct_StoresProductInDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateProductRequest
        {
            Name = "Test Lottery Ticket",
            Description = "Test Description",
            Type = "lottery_ticket",
            BasePrice = 100.00m,
            Currency = "USD",
            IsActive = true
        };

        // Act
        var result = await _productService.CreateProductAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(request.Name);
        result.BasePrice.Should().Be(request.BasePrice);

        var productInDb = await _dbContext.Products.FindAsync(result.Id);
        productInDb.Should().NotBeNull();
        productInDb!.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task ProductService_GetProductById_ReturnsProduct()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Test Product",
            Type = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _productService.GetProductByIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task ProductService_GetAllProducts_ReturnsOnlyActiveProducts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activeProduct = new Product
        {
            Name = "Active Product",
            Type = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        var inactiveProduct = new Product
        {
            Name = "Inactive Product",
            Type = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.AddRange(activeProduct, inactiveProduct);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _productService.GetAllProductsAsync(includeInactive: false);

        // Assert
        result.Should().Contain(p => p.Id == activeProduct.Id);
        result.Should().NotContain(p => p.Id == inactiveProduct.Id);
    }

    [Fact]
    public async Task ProductService_ValidateProductPurchase_ValidatesQuantity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Test Product",
            Type = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            MaxQuantityPerUser = 5,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        _mockHandlerRegistry
            .Setup(x => x.GetHandler("lottery_ticket"))
            .Returns((IProductHandler?)null); // No handler for simplicity

        // Act
        var result = await _productService.ValidateProductPurchaseAsync(product.Id, 10, userId); // Over limit

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Maximum quantity"));
    }

    [Fact]
    public async Task ProductService_ValidateProductPurchase_ValidatesAvailabilityDates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Future Product",
            Type = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            AvailableFrom = DateTime.UtcNow.AddDays(1), // Not yet available
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        _mockHandlerRegistry
            .Setup(x => x.GetHandler("lottery_ticket"))
            .Returns((IProductHandler?)null);

        // Act
        var result = await _productService.ValidateProductPurchaseAsync(product.Id, 1, userId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not yet available"));
    }

    [Fact]
    public async Task ProductService_CalculateProductPrice_ReturnsCorrectPrice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Test Product",
            Type = "lottery_ticket",
            BasePrice = 25.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        _mockHandlerRegistry
            .Setup(x => x.GetHandler("lottery_ticket"))
            .Returns((IProductHandler?)null);

        // Act
        var price = await _productService.CalculateProductPriceAsync(product.Id, 3, userId);

        // Assert
        price.Should().Be(75.00m); // 25.00 * 3
    }

    [Fact]
    public async Task ProductService_UpdateProduct_UpdatesProductInDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Original Name",
            Type = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var updateRequest = new UpdateProductRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Type = "lottery_ticket",
            BasePrice = 75.00m,
            Currency = "USD",
            IsActive = true
        };

        // Act
        var result = await _productService.UpdateProductAsync(product.Id, updateRequest, userId);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.BasePrice.Should().Be(75.00m);

        var productInDb = await _dbContext.Products.FindAsync(product.Id);
        productInDb!.Name.Should().Be("Updated Name");
        productInDb.BasePrice.Should().Be(75.00m);
    }

    [Fact]
    public async Task ProductService_DeleteProduct_SoftDeletesProduct()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Product To Delete",
            Type = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Act
        await _productService.DeleteProductAsync(product.Id, userId);

        // Assert
        var productInDb = await _dbContext.Products.FindAsync(product.Id);
        productInDb!.IsDeleted.Should().BeTrue();
        productInDb.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task LotteryTicketProductHandler_ValidatePurchase_CallsLotteryService()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var quantity = 2;

        var product = new Product
        {
            Id = productId,
            Name = "Lottery Ticket",
            Type = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            Metadata = JsonSerializer.Serialize(new { houseId = houseId.ToString() }),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var mockResponse = new AmesaBackend.Shared.Contracts.ApiResponse<object>
        {
            Success = true,
            Data = new { isValid = true }
        };

        _mockHttpRequest
            .Setup(x => x.PostRequest<AmesaBackend.Shared.Contracts.ApiResponse<object>>(
                It.IsAny<string>(),
                It.IsAny<object>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _lotteryHandler.ValidatePurchaseAsync(productId, quantity, userId, _dbContext);

        // Assert
        result.IsValid.Should().BeTrue();
        _mockHttpRequest.Verify(
            x => x.PostRequest<AmesaBackend.Shared.Contracts.ApiResponse<object>>(
                It.Is<string>(url => url.Contains("/tickets/validate")),
                It.IsAny<object>()),
            Times.Once);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}


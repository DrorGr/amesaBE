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

        _productService = new ProductService(_dbContext, _mockProductLogger.Object);
        _lotteryHandler = new LotteryTicketProductHandler(_mockHandlerLogger.Object, _mockHttpRequest.Object);
    }

    [Fact]
    public async Task ProductService_CreateProduct_StoresProductInDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateProductRequest
        {
            Code = "TEST-LOTTERY-001",
            Name = "Test Lottery Ticket",
            Description = "Test Description",
            ProductType = "lottery_ticket",
            BasePrice = 100.00m,
            Currency = "USD"
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
            Code = "TEST-PRODUCT-001",
            Name = "Test Product",
            ProductType = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _productService.GetProductAsync(product.Id);

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
            Code = "ACTIVE-PRODUCT-001",
            Name = "Active Product",
            ProductType = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var inactiveProduct = new Product
        {
            Code = "INACTIVE-PRODUCT-001",
            Name = "Inactive Product",
            ProductType = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.AddRange(activeProduct, inactiveProduct);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _productService.GetActiveProductsAsync();

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
            Code = "TEST-PRODUCT-002",
            Name = "Test Product",
            ProductType = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            MaxQuantityPerUser = 5,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
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
            Code = "FUTURE-PRODUCT-001",
            Name = "Future Product",
            ProductType = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            AvailableFrom = DateTime.UtcNow.AddDays(1), // Not yet available
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
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
            Code = "TEST-PRODUCT-004",
            Name = "Test Product",
            ProductType = "lottery_ticket",
            BasePrice = 25.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        _mockHandlerRegistry
            .Setup(x => x.GetHandler("lottery_ticket"))
            .Returns((IProductHandler?)null);

        // Act
        var price = await _productService.CalculatePriceAsync(product.Id, 3, userId);

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
            Code = "ORIGINAL-PRODUCT-001",
            Name = "Original Name",
            ProductType = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var updateRequest = new UpdateProductRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
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
            Code = "DELETE-PRODUCT-001",
            Name = "Product To Delete",
            ProductType = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Act
        await _productService.DeactivateProductAsync(product.Id, userId);

        // Assert
        var productInDb = await _dbContext.Products.FindAsync(product.Id);
        productInDb!.IsActive.Should().BeFalse();
        productInDb.Status.Should().Be("inactive");
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
            Code = "LOTTERY-TICKET-001",
            Name = "Lottery Ticket",
            ProductType = "lottery_ticket",
            BasePrice = 50.00m,
            Currency = "USD",
            IsActive = true,
            ProductMetadata = JsonSerializer.Serialize(new { houseId = houseId.ToString() }),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        
        // Add ProductLink so handler can find the house link
        var productLink = new ProductLink
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            LinkedEntityType = "house",
            LinkedEntityId = houseId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ProductLinks.Add(productLink);
        await _dbContext.SaveChangesAsync();

        var mockResponse = new AmesaBackend.Shared.Contracts.ApiResponse<object>
        {
            IsError = false,
            Code = 200,
            Message = "Success",
            Data = new { isValid = true }
        };

        _mockHttpRequest
            .Setup(x => x.PostRequest<AmesaBackend.Shared.Contracts.ApiResponse<object>>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _lotteryHandler.ValidatePurchaseAsync(productId, quantity, userId, _dbContext);

        // Assert
        result.IsValid.Should().BeTrue();
        _mockHttpRequest.Verify(
            x => x.PostRequest<AmesaBackend.Shared.Contracts.ApiResponse<object>>(
                It.Is<string>(url => url.Contains("/tickets/validate")),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<string>()),
            Times.Once);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}


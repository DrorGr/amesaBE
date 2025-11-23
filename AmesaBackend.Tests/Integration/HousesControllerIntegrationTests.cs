using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using AmesaBackend.Data;
using AmesaBackend.Models;
using AmesaBackend.DTOs;
using AmesaBackend.Controllers;
using AmesaBackend.Tests.TestHelpers;
using AmesaBackend.Tests.TestFixtures;
using FluentAssertions;

namespace AmesaBackend.Tests.Integration;

[Collection("IntegrationTests")]
public class HousesControllerIntegrationTests : IClassFixture<WebApplicationFixture>, IDisposable
{
    private readonly WebApplicationFixture _fixture;
    private readonly HttpClient _client;
    private readonly AmesaDbContext _context;

    public HousesControllerIntegrationTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
        _context = fixture.DbContext;
        
        // Seed test data using TestDataBuilder
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
        
        var users = new List<User>
        {
            TestDataBuilder.User().WithEmail("user1@test.com").Build(),
            TestDataBuilder.User().WithEmail("user2@test.com").Build()
        };
        _context.Users.AddRange(users);
        
        var houses = new List<House>
        {
            TestDataBuilder.House().WithStatus(LotteryStatus.Active).WithPrice(300000).Build(),
            TestDataBuilder.House().WithStatus(LotteryStatus.Active).WithPrice(500000).Build(),
            TestDataBuilder.House().WithStatus(LotteryStatus.Upcoming).WithPrice(400000).Build()
        };
        _context.Houses.AddRange(houses);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetHouses_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/houses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHouses_ReturnsPagedResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/houses?page=1&limit=10");
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<HouseDto>>>();

        // Assert
        response.EnsureSuccessStatusCode();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHouses_WithStatusFilter_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/houses?status=Active");
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<HouseDto>>>();

        // Assert
        response.EnsureSuccessStatusCode();
        content.Should().NotBeNull();
        content!.Data.Items.Should().OnlyContain(h => h.Status.ToLower() == "active");
    }

    [Fact]
    public async Task GetHouses_WithPriceFilter_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/houses?minPrice=200000&maxPrice=600000");
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<HouseDto>>>();

        // Assert
        response.EnsureSuccessStatusCode();
        content.Should().NotBeNull();
        content!.Data.Items.Should().OnlyContain(h => h.Price >= 200000 && h.Price <= 600000);
    }

    [Fact]
    public async Task GetHouseById_WithValidId_ReturnsHouse()
    {
        // Arrange
        var house = await _context.Houses.FirstAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/houses/{house.Id}");
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<HouseDto>>();

        // Assert
        response.EnsureSuccessStatusCode();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data.Id.Should().Be(house.Id);
    }

    [Fact]
    public async Task GetHouseById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/houses/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHouses_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var page1Response = await _client.GetAsync("/api/v1/houses?page=1&limit=2");
        var page1Content = await page1Response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<HouseDto>>>();

        var page2Response = await _client.GetAsync("/api/v1/houses?page=2&limit=2");
        var page2Content = await page2Response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<HouseDto>>>();

        // Assert
        page1Response.EnsureSuccessStatusCode();
        page2Response.EnsureSuccessStatusCode();
        page1Content!.Data.Items.Should().HaveCount(2);
        page2Content!.Data.Items.Should().HaveCount(1);
        page1Content.Data.Items.Should().NotBeEquivalentTo(page2Content.Data.Items);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
    }
}

// Collection attribute to ensure tests run sequentially
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<WebApplicationFixture>
{
}


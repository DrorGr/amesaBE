extern alias ContentApp;
using ContentProgram = ContentApp::Program;
using ContentApp::AmesaBackend.Content.Data;
using ContentApp::AmesaBackend.Content.DTOs;
using ContentApp::AmesaBackend.Content.Models;
using AmesaBackend.Shared.Caching;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;

namespace AmesaBackend.Tests.Integration;

[Collection("ContentIntegrationTests")]
public class TranslationsControllerIntegrationTests : IClassFixture<ContentWebApplicationFixture>, IDisposable
{
    private readonly ContentWebApplicationFixture _fixture;
    private readonly HttpClient _client;
    private readonly ContentDbContext _context;
    private readonly ICache _cache;

    public TranslationsControllerIntegrationTests(ContentWebApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
        _context = fixture.DbContext;
        _cache = fixture.Services.GetRequiredService<ICache>();
        
        // Seed test data
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
        
        // Add test language
        var language = new Language
        {
            Code = "en",
            Name = "English",
            NativeName = "English",
            IsActive = true,
            IsDefault = true,
            DisplayOrder = 1
        };
        _context.Languages.Add(language);
        
        // Add test translations
        var translations = new List<Translation>
        {
            new Translation
            {
                Id = Guid.NewGuid(),
                LanguageCode = "en",
                Key = "test.key1",
                Value = "Test Value 1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Translation
            {
                Id = Guid.NewGuid(),
                LanguageCode = "en",
                Key = "test.key2",
                Value = "Test Value 2",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        _context.Translations.AddRange(translations);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetTranslations_UsesRedisCache_ReturnsCachedResponse()
    {
        // Arrange
        var cacheKey = "translations_en";
        
        // Act - First request (cache miss)
        var response1 = await _client.GetAsync("/api/v1/translations/en");
        response1.EnsureSuccessStatusCode();
        var content1 = await response1.Content.ReadFromJsonAsync<ApiResponse<TranslationsResponseDto>>();
        content1.Should().NotBeNull();
        content1!.Success.Should().BeTrue();
        content1.Data.Should().NotBeNull();
        
        // Verify cache was set
        var cached = await _cache.GetRecordAsync<TranslationsResponseDto>(cacheKey);
        cached.Should().NotBeNull();
        cached!.Translations.Should().HaveCount(2);
        cached.Translations.Should().ContainKey("test.key1");
        cached.Translations.Should().ContainKey("test.key2");
        
        // Act - Second request (cache hit)
        var response2 = await _client.GetAsync("/api/v1/translations/en");
        response2.EnsureSuccessStatusCode();
        var content2 = await response2.Content.ReadFromJsonAsync<ApiResponse<TranslationsResponseDto>>();
        
        // Assert - Both responses should be identical
        content2.Should().NotBeNull();
        content2!.Data.Should().NotBeNull();
        content2.Data!.Translations.Should().BeEquivalentTo(content1.Data!.Translations);
    }

    [Fact]
    public async Task GetLanguages_UsesRedisCache_ReturnsCachedResponse()
    {
        // Arrange
        var cacheKey = "languages_list";
        
        // Act - First request (cache miss)
        var response1 = await _client.GetAsync("/api/v1/translations/languages");
        response1.EnsureSuccessStatusCode();
        var content1 = await response1.Content.ReadFromJsonAsync<ApiResponse<List<LanguageDto>>>();
        content1.Should().NotBeNull();
        content1!.Success.Should().BeTrue();
        content1.Data.Should().NotBeNull();
        
        // Verify cache was set
        var cached = await _cache.GetRecordAsync<List<LanguageDto>>(cacheKey);
        cached.Should().NotBeNull();
        cached!.Should().HaveCount(1);
        cached[0].Code.Should().Be("en");
        
        // Act - Second request (cache hit)
        var response2 = await _client.GetAsync("/api/v1/translations/languages");
        response2.EnsureSuccessStatusCode();
        var content2 = await response2.Content.ReadFromJsonAsync<ApiResponse<List<LanguageDto>>>();
        
        // Assert - Both responses should be identical
        content2.Should().NotBeNull();
        content2!.Data.Should().NotBeNull();
        content2.Data!.Should().BeEquivalentTo(content1.Data!);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
    }
}

/// <summary>
/// Fixture for Content service integration tests
/// </summary>
public class ContentWebApplicationFixture : IDisposable
{
    public WebApplicationFactory<ContentProgram> Factory { get; }
    public HttpClient Client { get; }
    public ContentDbContext DbContext { get; }
    public IServiceProvider Services { get; }
    private readonly IServiceScope _scope;
    private readonly string _databaseName = $"ContentTestDb_{Guid.NewGuid()}";

    public ContentWebApplicationFixture()
    {
        var databaseName = _databaseName;
        Factory = new WebApplicationFactory<ContentProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ContentDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database
                    services.AddDbContext<ContentDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });

                    // Remove existing ICache registration if present
                    var cacheDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ICache));
                    if (cacheDescriptor != null)
                    {
                        services.Remove(cacheDescriptor);
                    }

                    // Add in-memory cache mock for tests
                    services.AddSingleton<ICache, AmesaBackend.Tests.TestHelpers.InMemoryCache>();
                });
            });

        Client = Factory.CreateClient();
        _scope = Factory.Services.CreateScope();
        Services = _scope.ServiceProvider;
        DbContext = _scope.ServiceProvider.GetRequiredService<ContentDbContext>();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        _scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }
}

// Collection attribute to ensure tests run sequentially
[CollectionDefinition("ContentIntegrationTests")]
public class ContentIntegrationTestsCollection : ICollectionFixture<ContentWebApplicationFixture>
{
}


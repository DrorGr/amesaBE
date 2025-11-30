extern alias MainApp;
using MainProgram = MainApp::Program;
using MainApp::AmesaBackend.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.TestFixtures;

// Program class is in the root namespace of AmesaBackend project

/// <summary>
/// Fixture for creating test web applications with in-memory database
/// </summary>
public class WebApplicationFixture : IDisposable
{
    public WebApplicationFactory<MainProgram> Factory { get; }
    public HttpClient Client { get; }
    public AmesaDbContext DbContext { get; }
    public IServiceProvider Services { get; }
    private readonly IServiceScope _scope;
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    public WebApplicationFixture()
    {
        var databaseName = _databaseName; // Capture for closure
        Factory = new WebApplicationFactory<MainProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AmesaDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database with shared name so controller and test use same database
                    services.AddDbContext<AmesaDbContext>(options =>
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
                    services.AddSingleton<ICache, InMemoryCache>();
                });
            });

        Client = Factory.CreateClient();
        _scope = Factory.Services.CreateScope();
        Services = _scope.ServiceProvider;
        DbContext = _scope.ServiceProvider.GetRequiredService<AmesaDbContext>();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        _scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }
}


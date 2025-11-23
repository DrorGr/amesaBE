using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AmesaBackend.Data;

namespace AmesaBackend.Tests.TestFixtures;

// Program class is in the root namespace of AmesaBackend project

/// <summary>
/// Fixture for creating test web applications with in-memory database
/// </summary>
public class WebApplicationFixture : IDisposable
{
    public WebApplicationFactory<Program> Factory { get; }
    public HttpClient Client { get; }
    public AmesaDbContext DbContext { get; }
    private readonly IServiceScope _scope;

    public WebApplicationFixture()
    {
        Factory = new WebApplicationFactory<Program>()
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

                    // Add in-memory database
                    services.AddDbContext<AmesaDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

        Client = Factory.CreateClient();
        _scope = Factory.Services.CreateScope();
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


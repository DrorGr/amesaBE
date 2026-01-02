using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AmesaBackend.Lottery.Configuration;

public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog logging for the Lottery service.
    /// </summary>
    public static IHostBuilder UseLotterySerilog(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/lottery-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        return hostBuilder.UseSerilog();
    }

    /// <summary>
    /// Configures graceful shutdown for the application.
    /// </summary>
    public static IApplicationBuilder ConfigureGracefulShutdown(this IApplicationBuilder app)
    {
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(() =>
        {
            Log.Information("Application is shutting down...");
            Log.CloseAndFlush();
        });

        return app;
    }
}

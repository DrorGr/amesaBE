using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using AmesaBackend.Shared.Middleware.Logging;
using Serilog.Exceptions;

namespace AmesaBackend.Shared.Logging
{
    public static class AMESASerilogApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds serilog as the logging framework for this Host. 
        /// A configuration file named 'serilog.json' can be on project top level folder
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHostBuilder UseAMESASerilog(
            this IHostBuilder builder)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithExceptionDetails()
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.UseSerilog();

            return builder;
        }

        public static IApplicationBuilder UseAMESASerilog(
           this IApplicationBuilder builder)
        {
            builder.UseMiddleware<AMESASerilogScopedLoggingMiddleware>();
            builder.UseSerilogRequestLogging();

            return builder;
        }
    }
}


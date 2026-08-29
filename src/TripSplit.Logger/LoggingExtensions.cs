using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace TripSplit.Logger;

public static class LoggingExtensions
{
    public static IServiceCollection AddTripSplitLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var serilogLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        Log.Logger = serilogLogger;

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        return services;
    }
}

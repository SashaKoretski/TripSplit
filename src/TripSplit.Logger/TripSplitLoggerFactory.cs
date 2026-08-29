using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace TripSplit.Logger;

public static class TripSplitLoggerFactory
{
    public static ILoggerFactory Create(IConfiguration configuration)
    {
        var serilog = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        return new SerilogLoggerFactory(serilog, dispose: true);
    }
}

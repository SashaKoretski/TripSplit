using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Services;
using TripSplit.ConsoleUI.Commands;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Repositories;
using TripSplit.Logger;

// Composition Root
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(config);

// Logging (Serilog via Microsoft.Extensions.Logging)
services.AddTripSplitLogging(config);

// Business logic options
services.AddSingleton(new DebtSettlementOptions
{
    MinTransferAmount = config.GetValue<decimal?>("DebtSettlement:MinTransferAmount") ?? 0.01m
});

// Data Access
services.AddSingleton<IDbConnectionFactory>(_ =>
    new NpgsqlConnectionFactory(
        config.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres is missing")));

services.AddSingleton<IUserRepository, UserRepository>();
services.AddSingleton<ITripRepository, TripRepository>();
services.AddSingleton<IExpenseRepository, ExpenseRepository>();
services.AddSingleton<IReceiptRepository, ReceiptRepository>();

// Business Logic
services.AddSingleton<IUserService, UserService>();
services.AddSingleton<ITripService, TripService>();
services.AddSingleton<IExpenseService, ExpenseService>();
services.AddSingleton<IReceiptService, ReceiptService>();
services.AddSingleton<IDebtMinimizationStrategy, GreedyDebtMinimizationStrategy>();
services.AddSingleton<IDebtSettlementService, DebtSettlementService>();

// Console UI infrastructure
services.AddSingleton<IConsoleIO, ConsoleIO>();
services.AddSingleton<IAppSession, AppSession>();
services.AddSingleton<ISessionRefresher, TripSessionRefresher>();
services.AddSingleton<MenuRunner>();

// Commands: конкретный класс регистрируется отдельно, а как IMenuItem
// возвращается декоратор с логированием.
RegisterMenuItem<LoginCommand>(services);
RegisterMenuItem<CreateTripCommand>(services);
RegisterMenuItem<JoinTripCommand>(services);
RegisterMenuItem<SelectTripCommand>(services);
RegisterMenuItem<InviteParticipantCommand>(services);
RegisterMenuItem<ViewExpensesCommand>(services);
RegisterMenuItem<ViewStatisticsCommand>(services);
RegisterMenuItem<ViewDebtsCommand>(services);
RegisterMenuItem<AddExpenseCommand>(services);
RegisterMenuItem<DeleteExpenseCommand>(services);
RegisterMenuItem<AddReceiptCommand>(services);
RegisterMenuItem<FinishTripCommand>(services);

await using var provider = services.BuildServiceProvider();

try
{
    var runner = provider.GetRequiredService<MenuRunner>();
    await runner.RunAsync();
}
catch (Exception ex)
{
    var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("TripSplit.ConsoleUI");
    logger.LogCritical(ex, "Unhandled exception, application terminated");
    Environment.ExitCode = 1;
}
finally
{
    Serilog.Log.CloseAndFlush();
}

// Регистрирует команду как concrete singleton и оборачивает ее LoggingMenuItemDecorator
// при разрешении через интерфейс IMenuItem.
static void RegisterMenuItem<TCommand>(IServiceCollection services)
    where TCommand : class, IMenuItem
{
    services.AddSingleton<TCommand>();
    services.AddSingleton<IMenuItem>(sp => new LoggingMenuItemDecorator(
        sp.GetRequiredService<TCommand>(),
        sp.GetRequiredService<IAppSession>(),
        sp.GetRequiredService<ILogger<LoggingMenuItemDecorator>>()));
}

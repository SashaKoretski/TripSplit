using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Polling;
using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Services;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Repositories;
using TripSplit.DataAccess.Storage;
using TripSplit.Logger;
using TripSplit.TelegramBot.Commands;
using TripSplit.TelegramBot.Configuration;
using TripSplit.TelegramBot.Identity;
using TripSplit.TelegramBot.Notifications;
using TripSplit.TelegramBot.UpdateHandling;

// Composition Root
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(config);

services.AddTripSplitLogging(config);

services.AddSingleton(new DebtSettlementOptions
{
    MinTransferAmount = config.GetValue<decimal?>("DebtSettlement:MinTransferAmount") ?? 0.01m
});

services.AddSingleton(new ReceiptImageOptions());

services.AddSingleton(new S3StorageOptions
{
    ServiceUrl = config["ObjectStorage:ServiceUrl"]
        ?? throw new InvalidOperationException("ObjectStorage:ServiceUrl is missing"),
    AccessKey = config["ObjectStorage:AccessKey"]
        ?? throw new InvalidOperationException("ObjectStorage:AccessKey is missing"),
    SecretKey = config["ObjectStorage:SecretKey"]
        ?? throw new InvalidOperationException("ObjectStorage:SecretKey is missing"),
    Bucket = config["ObjectStorage:Bucket"]
        ?? throw new InvalidOperationException("ObjectStorage:Bucket is missing")
});

var telegramOptions = new TelegramOptions
{
    BotToken = config["Telegram:BotToken"]
        ?? throw new InvalidOperationException(
            "Telegram:BotToken is missing. Run: dotnet user-secrets set \"Telegram:BotToken\" \"<token>\""),
    BotUsername = config["Telegram:BotUsername"]
        ?? throw new InvalidOperationException("Telegram:BotUsername is missing")
};
services.AddSingleton(telegramOptions);

// Data Access
services.AddSingleton<IDbConnectionFactory>(_ =>
    new NpgsqlConnectionFactory(
        config.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres is missing")));

services.AddSingleton<IUserRepository, UserRepository>();
services.AddSingleton<ITripRepository, TripRepository>();
services.AddSingleton<IExpenseRepository, ExpenseRepository>();
services.AddSingleton<IReceiptRepository, ReceiptRepository>();
services.AddSingleton<IReceiptImageRepository, ReceiptImageRepository>();

// Object storage
services.AddSingleton<IFileStorageService, S3FileStorageService>();

// Business Logic
services.AddSingleton<IUserService, UserService>();
services.AddSingleton<ITripService, TripService>();
services.AddSingleton<IExpenseService, ExpenseService>();
services.AddSingleton<IReceiptImageService, ReceiptImageService>();
services.AddSingleton<IReceiptService, ReceiptService>();
services.AddSingleton<IDebtMinimizationStrategy, GreedyDebtMinimizationStrategy>();
services.AddSingleton<IDebtSettlementService, DebtSettlementService>();

// Telegram bot infrastructure
services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(telegramOptions.BotToken));
services.AddSingleton<IBotSessionStore, BotSessionStore>();
services.AddSingleton<ITelegramUserResolver, TelegramUserResolver>();
services.AddSingleton<ITripNotifier, TripNotifier>();
services.AddSingleton<UpdateDispatcher>();

services.AddSingleton<ICommandHandler, StartCommandHandler>();
services.AddSingleton<ICommandHandler, HelpCommandHandler>();
services.AddSingleton<ICommandHandler, TripsCommandHandler>();
services.AddSingleton<ICommandHandler, NewTripCommandHandler>();
services.AddSingleton<ICommandHandler, InviteCommandHandler>();
services.AddSingleton<ICommandHandler, ExpensesCommandHandler>();
services.AddSingleton<ICommandHandler, AddExpenseCommandHandler>();
services.AddSingleton<ICommandHandler, NewReceiptCommandHandler>();
services.AddSingleton<ICommandHandler, ReceiptsCommandHandler>();
services.AddSingleton<ICommandHandler, SettlementCommandHandler>();
services.AddSingleton<ICommandHandler, FinishCommandHandler>();

await using var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("TripSplit.TelegramBot");

try
{
    var bot = provider.GetRequiredService<ITelegramBotClient>();
    var dispatcher = provider.GetRequiredService<UpdateDispatcher>();

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    var receiverOptions = new ReceiverOptions
    {
        AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
    };

    bot.StartReceiving(dispatcher, receiverOptions, cts.Token);

    var me = await bot.GetMeAsync(cts.Token);
    logger.LogInformation("TripSplit bot @{Username} started", me.Username);
    Console.WriteLine($"Бот @{me.Username} запущен. Нажмите Ctrl+C для остановки.");

    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Bot stopped");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Unhandled exception, application terminated");
    Environment.ExitCode = 1;
}
finally
{
    Serilog.Log.CloseAndFlush();
}

using Microsoft.Extensions.Configuration;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Services;
using TripSplit.ConsoleUI.Commands;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Repositories;

// Composition Root
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(config);

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

// Console UI
services.AddSingleton<IConsoleIO, ConsoleIO>();
services.AddSingleton<IAppSession, AppSession>();
services.AddSingleton<ISessionRefresher, TripSessionRefresher>();
services.AddSingleton<MenuRunner>();

services.AddSingleton<IMenuItem, LoginCommand>();
services.AddSingleton<IMenuItem, CreateTripCommand>();
services.AddSingleton<IMenuItem, JoinTripCommand>();
services.AddSingleton<IMenuItem, SelectTripCommand>();
services.AddSingleton<IMenuItem, InviteParticipantCommand>();
services.AddSingleton<IMenuItem, ViewExpensesCommand>();
services.AddSingleton<IMenuItem, ViewStatisticsCommand>();
services.AddSingleton<IMenuItem, ViewDebtsCommand>();
services.AddSingleton<IMenuItem, AddExpenseCommand>();
services.AddSingleton<IMenuItem, DeleteExpenseCommand>();
services.AddSingleton<IMenuItem, AddReceiptCommand>();
services.AddSingleton<IMenuItem, FinishTripCommand>();

await using var provider = services.BuildServiceProvider();
var runner = provider.GetRequiredService<MenuRunner>();
await runner.RunAsync();

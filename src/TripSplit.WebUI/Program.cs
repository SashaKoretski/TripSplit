using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Services;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Repositories;
using TripSplit.Logger;
using TripSplit.WebUI.Infrastructure;
using TripSplit.WebUI.Session;

// Composition Root
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTripSplitLogging(builder.Configuration);

builder.Services.AddSingleton(new DebtSettlementOptions
{
    MinTransferAmount = builder.Configuration.GetValue<decimal?>("DebtSettlement:MinTransferAmount") ?? 0.01m
});

// Data Access
builder.Services.AddSingleton<IDbConnectionFactory>(_ =>
    new NpgsqlConnectionFactory(
        builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres is missing")));

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<ITripRepository, TripRepository>();
builder.Services.AddSingleton<IExpenseRepository, ExpenseRepository>();
builder.Services.AddSingleton<IReceiptRepository, ReceiptRepository>();

// Business Logic
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ITripService, TripService>();
builder.Services.AddSingleton<IExpenseService, ExpenseService>();
builder.Services.AddSingleton<IReceiptService, ReceiptService>();
builder.Services.AddSingleton<IDebtMinimizationStrategy, GreedyDebtMinimizationStrategy>();
builder.Services.AddSingleton<IDebtSettlementService, DebtSettlementService>();

// Web-сессия
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = "TripSplit.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromHours(4);
});
builder.Services.AddScoped<IWebAppSession, WebAppSession>();

// MVC + глобальный фильтр доменных исключений
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add<DomainExceptionFilter>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

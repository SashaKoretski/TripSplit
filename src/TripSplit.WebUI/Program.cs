using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Services;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Repositories;
using TripSplit.DataAccess.Storage;
using TripSplit.Logger;
using TripSplit.WebUI.Configuration;
using TripSplit.WebUI.Infrastructure;
using TripSplit.WebUI.Infrastructure.Api;
using TripSplit.WebUI.Session;

// Composition Root
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTripSplitLogging(builder.Configuration);

builder.Services.AddSingleton(new DebtSettlementOptions
{
    MinTransferAmount = builder.Configuration.GetValue<decimal?>("DebtSettlement:MinTransferAmount") ?? 0.01m
});

builder.Services.AddSingleton(new ReceiptImageOptions());

builder.Services.AddSingleton(new S3StorageOptions
{
    ServiceUrl = builder.Configuration["ObjectStorage:ServiceUrl"]
        ?? throw new InvalidOperationException("ObjectStorage:ServiceUrl is missing"),
    AccessKey = builder.Configuration["ObjectStorage:AccessKey"]
        ?? throw new InvalidOperationException("ObjectStorage:AccessKey is missing"),
    SecretKey = builder.Configuration["ObjectStorage:SecretKey"]
        ?? throw new InvalidOperationException("ObjectStorage:SecretKey is missing"),
    Bucket = builder.Configuration["ObjectStorage:Bucket"]
        ?? throw new InvalidOperationException("ObjectStorage:Bucket is missing")
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
builder.Services.AddSingleton<IReceiptImageRepository, ReceiptImageRepository>();

// Object storage
builder.Services.AddSingleton<IFileStorageService, S3FileStorageService>();

// Business Logic
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ITripService, TripService>();
builder.Services.AddSingleton<IExpenseService, ExpenseService>();
builder.Services.AddSingleton<IReceiptImageService, ReceiptImageService>();
builder.Services.AddSingleton<IReceiptService, ReceiptService>();
builder.Services.AddSingleton<IDebtMinimizationStrategy, GreedyDebtMinimizationStrategy>();
builder.Services.AddSingleton<IDebtSettlementService, DebtSettlementService>();

// REST API (/api/v1) - JWT Bearer, отдельно от cookie-сессии Web UI (ADR-001)
var jwtOptions = new JwtOptions
{
    SigningKey = builder.Configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Jwt:SigningKey is missing"),
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "tripsplit-api",
    Audience = builder.Configuration["Jwt:Audience"] ?? "tripsplit-api",
    ExpiresInSeconds = builder.Configuration.GetValue<int?>("Jwt:ExpiresInSeconds") ?? 3600
};
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

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
})
    // enum'ы (ExpenseType) в JSON API - строками в camelCase ("food"), как в openapi.yaml,
    // а не числами по умолчанию
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Swagger UI раздает тот же YAML, что лежит в src/openapi/openapi.yaml (источник истины,
// WebLab#2) - без codegen, чтобы не завести вторую, расходящуюся спецификацию.
var openApiSpecPath = Path.Combine(builder.Environment.ContentRootPath, "..", "openapi", "openapi.yaml");
app.MapGet("/api-docs/openapi.yaml", () => Results.File(openApiSpecPath, "application/yaml"));
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/api-docs/openapi.yaml", "TripSplit API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// WebApplicationFactory<Program> в интеграционных тестах нужен публичный класс Program -
// top-level statements генерируют internal, этот partial делает его public без смены видимости.
public partial class Program { }

using System.Net;
using System.Text.RegularExpressions;
using TripSplit.DataAccess.Infrastructure;
using Xunit;

namespace TripSplit.E2E.Tests;

// E2E-тест демо/MVP-сценария TripSplit (ТИОПО ЛР2, З3): регистрация двух пользователей,
// создание поездки, приглашение участника, добавление траты, просмотр расчета долгов,
// завершение поездки - все через настоящие HTTP-запросы к развернутому приложению, без GUI
// (Т13) и без единого мока: WebUI в этом прогоне работает поверх настоящего контейнера
// postgres-test (см. docker-compose.yml), а не WebApplicationFactory/in-memory TestServer (Т6).
// Один файл - один тест из последовательных шагов Act/Assert (Т17.a/b), Arrange - один раз.
[Trait("Category", "E2E")]
public sealed class TripLifecycleE2ETests : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("TRIPSPLIT_E2E_BASE_URL") ?? "http://localhost:5299";

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TRIPSPLIT_TEST_DB")
        ?? "Host=localhost;Port=5433;Database=tripsplit_test;Username=tripsplit_test;Password=tripsplit_test";

    private readonly DatabaseInitializer _storage = new(new NpgsqlConnectionFactory(ConnectionString));

    // Откат хранилища к состоянию "до прогона" перед сценарием (Т4) и в аварийном/успешном
    // teardown после него (Т10) - тем же переиспользуемым скриптом, что и у integration-тестов.
    public Task InitializeAsync() => _storage.ResetAsync();
    public Task DisposeAsync() => _storage.ResetAsync();

    [Fact]
    public async Task RegisterTwoUsers_CreateTrip_AddExpense_FinishTrip_ShowsCorrectSettlement()
    {
        // Arrange - два независимых "браузера" (своя cookie-сессия у каждого), как у двух
        // реальных пользователей; завершение обеих сессий логаутом гарантировано в finally (Т12)
        using var sasha = new TestBrowser(BaseUrl);
        using var masha = new TestBrowser(BaseUrl);
        try
        {
            // Act/Assert 1: регистрация Саши
            var sashaLogin = await sasha.GetAsync("/Account/Login");
            Assert.Equal(HttpStatusCode.OK, sashaLogin.StatusCode);
            var sashaRegistered = await sasha.PostFormAsync("/Account/Register", sashaLogin.Body, new Dictionary<string, string>
            {
                ["Register.Name"] = "Sasha",
                ["Register.Email"] = "sasha@example.test"
            });
            Assert.Equal(HttpStatusCode.Redirect, sashaRegistered.StatusCode);

            // Act/Assert 2: регистрация Маши (отдельная сессия - свой cookie, не затирает Сашину)
            var mashaLogin = await masha.GetAsync("/Account/Login");
            var mashaRegistered = await masha.PostFormAsync("/Account/Register", mashaLogin.Body, new Dictionary<string, string>
            {
                ["Register.Name"] = "Masha",
                ["Register.Email"] = "masha@example.test"
            });
            Assert.Equal(HttpStatusCode.Redirect, mashaRegistered.StatusCode);

            // Act/Assert 3: Саша создает поездку
            var tripsIndex = await sasha.GetAsync("/Trips");
            var tripCreated = await sasha.PostFormAsync("/Trips/Create", tripsIndex.Body, new Dictionary<string, string>
            {
                ["Create.Name"] = "Weekend in Sochi",
                ["Create.Currency"] = "rub"
            });
            Assert.Equal(HttpStatusCode.Redirect, tripCreated.StatusCode);
            var tripId = TestBrowser.FindFirstGuidInPath(tripCreated.Location);

            // Act/Assert 4: Саша приглашает Машу по email
            var details1 = await sasha.GetAsync($"/Trips/Details/{tripId}");
            Assert.Contains("Sasha", details1.Body);
            var invited = await sasha.PostFormAsync($"/Trips/Invite/{tripId}", details1.Body, new Dictionary<string, string>
            {
                ["Invite.Email"] = "masha@example.test"
            });
            Assert.Equal(HttpStatusCode.Redirect, invited.StatusCode);

            var details2 = await sasha.GetAsync($"/Trips/Details/{tripId}");
            Assert.DoesNotContain("alert-error", details2.Body);
            Assert.Contains("Masha", details2.Body);

            // Act/Assert 5: Саша добавляет трату (ужин на двоих, со скидкой)
            var createExpensePage = await sasha.GetAsync("/Expenses/Create");
            Assert.Equal(HttpStatusCode.OK, createExpensePage.StatusCode);
            var sashaId = TestBrowser.FindParticipantId(createExpensePage.Body, "Sasha");
            var mashaId = TestBrowser.FindParticipantId(createExpensePage.Body, "Masha");

            var expenseFields = new List<KeyValuePair<string, string>>
            {
                new("Name", "Dinner"),
                new("Type", "Food"),
                new("Value", "1000"),
                new("Discount", "200"),
                new("PayerId", sashaId.ToString()),
                new("ConsumerIds", sashaId.ToString()),
                new("ConsumerIds", mashaId.ToString()) // два чекбокса с одинаковым именем
            };
            var expenseCreated = await sasha.PostFormAsync("/Expenses/Create", createExpensePage.Body, expenseFields);
            Assert.Equal(HttpStatusCode.Redirect, expenseCreated.StatusCode);

            var expensesIndex = await sasha.GetAsync("/Expenses");
            Assert.Contains("Dinner", expensesIndex.Body);
            Assert.True(
                Regex.IsMatch(expensesIndex.Body, "800[.,]00"),
                "Expected effective amount 800 (1000 - 200 discount) on the expenses list page");

            // Act/Assert 6: расчет долгов показывает перевод Маша -> Саша на половину суммы
            var settlement = await sasha.GetAsync("/Settlement");
            Assert.Equal(HttpStatusCode.OK, settlement.StatusCode);
            Assert.Contains("Masha", settlement.Body);
            Assert.Contains("Sasha", settlement.Body);
            Assert.True(
                Regex.IsMatch(settlement.Body, "400[.,]00"),
                "Expected a 400 transfer (half of the 800 effective dinner amount) on the settlement page");

            // Act/Assert 7: завершение поездки
            var finishPage = await sasha.GetAsync($"/Trips/Details/{tripId}");
            var finished = await sasha.PostFormAsync($"/Trips/Finish/{tripId}", finishPage.Body, new Dictionary<string, string>());
            Assert.Equal(HttpStatusCode.Redirect, finished.StatusCode);

            var details3 = await sasha.GetAsync($"/Trips/Details/{tripId}");
            Assert.Contains("status-finished", details3.Body.ToLowerInvariant());

            // Act/Assert 8 (авария): завершенная поездка больше не принимает новые траты -
            // сервер отклоняет запрос редиректом с ошибкой, а не 500/необработанным исключением
            var createAfterFinish = await sasha.GetAsync("/Expenses/Create");
            Assert.Equal(HttpStatusCode.Redirect, createAfterFinish.StatusCode);
            Assert.Contains("/Expenses", createAfterFinish.Location!.ToString());
        }
        finally
        {
            // Т12: завершить обе пользовательские сессии после сценария - успешного или нет
            await LogoutIfPossibleAsync(sasha);
            await LogoutIfPossibleAsync(masha);
        }
    }

    private static async Task LogoutIfPossibleAsync(TestBrowser browser)
    {
        var page = await browser.GetAsync("/Trips");
        if (page.StatusCode != HttpStatusCode.OK) return;
        await browser.PostFormAsync("/Account/Logout", page.Body, new Dictionary<string, string>());
    }
}

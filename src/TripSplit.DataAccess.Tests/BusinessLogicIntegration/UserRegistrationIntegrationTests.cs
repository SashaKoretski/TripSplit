using System.Data.Common;
using TripSplit.BusinessLogic.Services;
using TripSplit.DataAccess.Repositories;
using Xunit;

namespace TripSplit.DataAccess.Tests.BusinessLogicIntegration;

// Integration-тест: правило дедупликации UserService.RegisterAsync (по googleId, затем по email)
// должно согласовываться с реальными UNIQUE-constraint'ами таблицы users в Postgres - проверяем
// и возвращаемый аккаунт, и фактическое число строк в хранилище.
[Trait("Category", "Integration")]
[Collection("Database")]
public sealed class UserRegistrationIntegrationTests
{
    private readonly UserService _users;
    private readonly DatabaseFixture _fixture;

    public UserRegistrationIntegrationTests(DatabaseFixture db)
    {
        _users = new UserService(new UserRepository(db.Factory));
        _fixture = db;
    }

    [Fact]
    public async Task RegisterAsync_DuplicateGoogleIdOrEmail_ReturnsExistingAccountWithoutExtraRow()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act/Assert 1: первая регистрация создает ровно одну строку
        var first = await _users.RegisterAsync("Sasha", "sasha@example.test", "gid-sasha-reg");
        Assert.Equal(1, await CountUsersWithEmailAsync("sasha@example.test"));

        // Act/Assert 2: повторная регистрация с тем же googleId возвращает тот же аккаунт,
        // новой строки не появляется
        var sameGoogleId = await _users.RegisterAsync("Sasha Duplicate", "sasha@example.test", "gid-sasha-reg");
        Assert.Equal(first.Id, sameGoogleId.Id);
        Assert.Equal(1, await CountUsersWithEmailAsync("sasha@example.test"));

        // Act/Assert 3: другой googleId, но та же почта (другой регистр) - дедуп по email,
        // новой строки тоже не появляется
        var sameEmailDifferentCase = await _users.RegisterAsync(
            "Sasha Third Name", "SASHA@EXAMPLE.TEST", "gid-sasha-reg-another");
        Assert.Equal(first.Id, sameEmailDifferentCase.Id);
        Assert.Equal(1, await CountUsersWithEmailAsync("sasha@example.test"));

        // Act/Assert 4: действительно новый пользователь с другой почтой создает новую строку
        var masha = await _users.RegisterAsync("Masha", "masha@example.test", "gid-masha-reg");
        Assert.NotEqual(first.Id, masha.Id);
        Assert.Equal(1, await CountUsersWithEmailAsync("masha@example.test"));
    }

    private async Task<long> CountUsersWithEmailAsync(string email)
    {
        await using var conn = await _fixture.Factory.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM users WHERE email = @email";
        var p = cmd.CreateParameter();
        p.ParameterName = "email";
        p.Value = email;
        cmd.Parameters.Add(p);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
}

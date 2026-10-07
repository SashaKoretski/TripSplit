using Moq;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Repositories;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Repositories;

/// <summary>
/// Истинные unit-тесты UserRepository: ADO.NET (DbConnection/DbCommand/DbDataReader) подделан
/// в памяти через TestDoubles/FakeAdo.cs, реальная база данных не используется и не требуется
/// (в отличие от UserRepositoryTests.cs, которые являются integration-тестами против реального Postgres).
/// Техника данных: граничное значение Guid.Empty/строка из пробелов для всех guard-веток,
/// классы эквивалентности "строка найдена / не найдена" для позитивных путей.
/// </summary>
public sealed class UserRepositoryUnitTests
{
    private static User NewUser(string tag = "sasha") =>
        new(Guid.NewGuid(), $"User {tag}", $"{tag}@example.test", $"gid-{tag}");

    private static Dictionary<string, object?> RowOf(User u) => new()
    {
        ["id"] = u.Id,
        ["name"] = u.Name,
        ["email"] = u.Email,
        ["google_id"] = u.GoogleId,
    };

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_RowExists_ReturnsMappedUser()
    {
        var user = NewUser();
        var conn = new FakeDbConnection(FakeCommandScript.Reader(RowOf(user)));
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Email, result!.Email);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyId_ThrowsArgumentExceptionWithoutOpeningConnection()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new UserRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetByIdAsync(Guid.Empty));

        factory.Verify(f => f.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatchingRow_ReturnsNull()
    {
        var conn = new FakeDbConnection(FakeCommandScript.EmptyReader());
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // --- GetByGoogleIdAsync ---

    [Fact]
    public async Task GetByGoogleIdAsync_RowExists_ReturnsMappedUser()
    {
        var user = NewUser("masha");
        var conn = new FakeDbConnection(FakeCommandScript.Reader(RowOf(user)));
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByGoogleIdAsync(user.GoogleId);

        Assert.Equal(user.Id, result!.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByGoogleIdAsync_BlankGoogleId_ThrowsArgumentException(string googleId)
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new UserRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetByGoogleIdAsync(googleId));
    }

    // --- GetByEmailAsync ---

    [Fact]
    public async Task GetByEmailAsync_RowExists_ReturnsMappedUser()
    {
        var user = NewUser("dan");
        var conn = new FakeDbConnection(FakeCommandScript.Reader(RowOf(user)));
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByEmailAsync(user.Email);

        Assert.Equal(user.Id, result!.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByEmailAsync_BlankEmail_ThrowsArgumentException(string email)
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new UserRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetByEmailAsync(email));
    }

    // --- AddAsync ---

    [Fact]
    public async Task AddAsync_ValidUser_ExecutesInsertWithAllParameters()
    {
        var user = NewUser("egor");
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        await repo.AddAsync(user);

        var cmd = Assert.Single(conn.ExecutedCommands);
        Assert.Contains("INSERT INTO users", cmd.CommandText);
        Assert.Contains(cmd.CapturedParameters, p => p.Name == "id" && (Guid)p.Value! == user.Id);
        Assert.Contains(cmd.CapturedParameters, p => p.Name == "email" && (string)p.Value! == user.Email);
    }

    [Fact]
    public async Task AddAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new UserRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.AddAsync(null!));
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_RowAffected_CompletesSuccessfully()
    {
        var user = NewUser("pasha");
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        await repo.UpdateAsync(user);

        Assert.Contains("UPDATE users", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task UpdateAsync_NoRowAffected_ThrowsEntityNotFoundException()
    {
        var user = NewUser();
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        await Assert.ThrowsAsync<EntityNotFoundException>(() => repo.UpdateAsync(user));
    }

    [Fact]
    public async Task UpdateAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new UserRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.UpdateAsync(null!));
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_RowAffected_CompletesSuccessfully()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        await repo.DeleteAsync(Guid.NewGuid());

        Assert.Contains("DELETE FROM users", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task DeleteAsync_NoRowAffected_ThrowsEntityNotFoundException()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var repo = new UserRepository(new FakeDbConnectionFactory(conn));

        await Assert.ThrowsAsync<EntityNotFoundException>(() => repo.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_EmptyId_ThrowsArgumentExceptionWithoutOpeningConnection()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new UserRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.DeleteAsync(Guid.Empty));

        factory.Verify(f => f.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

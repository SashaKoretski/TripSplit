using Moq;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Repositories;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Repositories;

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

        var act = () => repo.GetByIdAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
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

        var act = () => repo.GetByGoogleIdAsync(googleId);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

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

        var act = () => repo.GetByEmailAsync(email);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

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

        var act = () => repo.AddAsync(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

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

        var act = () => repo.UpdateAsync(user);

        await Assert.ThrowsAsync<EntityNotFoundException>(act);
    }

    [Fact]
    public async Task UpdateAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new UserRepository(factory.Object);

        var act = () => repo.UpdateAsync(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

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

        var act = () => repo.DeleteAsync(Guid.NewGuid());

        await Assert.ThrowsAsync<EntityNotFoundException>(act);
    }

    [Fact]
    public async Task DeleteAsync_EmptyId_ThrowsArgumentExceptionWithoutOpeningConnection()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new UserRepository(factory.Object);

        var act = () => repo.DeleteAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
        factory.Verify(f => f.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Repositories;
using Xunit;

namespace TripSplit.DataAccess.Tests;

[Collection("Database")]
public sealed class UserRepositoryTests
{
    private readonly DatabaseFixture _db;
    private readonly UserRepository _repo;

    public UserRepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _repo = new UserRepository(db.Factory);
    }

    [Fact]
    public async Task Add_Then_GetById_Returns_Same_User()
    {
        await _db.ResetAsync();
        var user = NewUser("sasha");

        await _repo.AddAsync(user);
        var fetched = await _repo.GetByIdAsync(user.Id);

        Assert.NotNull(fetched);
        Assert.Equal(user.Id, fetched!.Id);
        Assert.Equal(user.Name, fetched.Name);
        Assert.Equal(user.Email, fetched.Email);
        Assert.Equal(user.GoogleId, fetched.GoogleId);
    }

    [Fact]
    public async Task Update_Changes_Are_Persisted()
    {
        await _db.ResetAsync();
        var user = NewUser("masha");
        await _repo.AddAsync(user);

        var updated = new User(user.Id, "Masha Renamed", "masha-new@example.com", user.GoogleId);
        await _repo.UpdateAsync(updated);

        var fetched = await _repo.GetByIdAsync(user.Id);
        Assert.Equal("Masha Renamed", fetched!.Name);
        Assert.Equal("masha-new@example.com", fetched.Email);
    }

    [Fact]
    public async Task Delete_Removes_User()
    {
        await _db.ResetAsync();
        var user = NewUser("dan");
        await _repo.AddAsync(user);

        await _repo.DeleteAsync(user.Id);

        Assert.Null(await _repo.GetByIdAsync(user.Id));
    }

    [Fact]
    public async Task AddAsync_Duplicate_Email_Throws_DuplicateKey()
    {
        await _db.ResetAsync();
        var first = NewUser("egor");
        var second = new User(Guid.NewGuid(), "Egor Two", first.Email, "gid-egor-2");

        await _repo.AddAsync(first);
        await Assert.ThrowsAsync<DuplicateKeyException>(() => _repo.AddAsync(second));
    }

    private static User NewUser(string tag) => new(
        id:       Guid.NewGuid(),
        name:     $"User {tag}",
        email:    $"{tag}@example.com",
        googleId: $"gid-{tag}");
}

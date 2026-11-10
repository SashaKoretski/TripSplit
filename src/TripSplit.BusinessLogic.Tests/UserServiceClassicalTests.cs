using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests;

[TestClass]
public class UserServiceClassicalTests
{
    private InMemoryUserRepository _users = null!;
    private UserService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _users = new InMemoryUserRepository();
        _sut = new UserService(_users);
    }

    [TestMethod]
    public async Task RegisterAsync_NewUser_CreatesAndPersists()
    {
        var user = await _sut.RegisterAsync("Dasha", "dasha@example.test", "gid-dasha");

        var persisted = await _users.GetByIdAsync(user.Id);
        Assert.IsNotNull(persisted);
        Assert.AreEqual("Dasha", persisted!.Name);
        Assert.AreEqual(1, _users.AddCallCount);
    }

    [TestMethod]
    public async Task RegisterAsync_ExistingGoogleId_ReturnsExistingWithoutAdding()
    {
        var existing = await _sut.RegisterAsync("Dasha", "dasha@example.test", "gid-dasha");

        var result = await _sut.RegisterAsync("New Name", "new@example.test", "gid-dasha");

        Assert.AreEqual(existing.Id, result.Id);
        Assert.AreEqual(1, _users.AddCallCount);
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFound_ThrowsUserNotFoundException()
    {
        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await Assert.ThrowsExactlyAsync<UserNotFoundException>(act);
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;

namespace TripSplit.BusinessLogic.Tests;

[TestClass]
public class UserServiceTests
{
    private Mock<IUserRepository> _users = null!;
    private UserService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _users = new Mock<IUserRepository>();
        _sut = new UserService(_users.Object);
    }

    [TestMethod]
    public void Ctor_NullRepository_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new UserService(null!));
    }

    [TestMethod]
    public async Task RegisterAsync_NewUser_CreatesAndPersists()
    {
        _users.Setup(r => r.GetByGoogleIdAsync("g-1")).ReturnsAsync((User?)null);

        var user = await _sut.RegisterAsync("a", "a@example.com", "g-1");

        Assert.IsNotNull(user);
        Assert.AreNotEqual(Guid.Empty, user.Id);
        Assert.AreEqual("a", user.Name);
        Assert.AreEqual("a@example.com", user.Email);
        Assert.AreEqual("g-1", user.GoogleId);
        _users.Verify(r => r.AddAsync(It.Is<User>(u => u.GoogleId == "g-1")), Times.Once);
    }

    [TestMethod]
    public async Task RegisterAsync_ExistingGoogleId_ReturnsExistingWithoutAdding()
    {
        var existing = new User(Guid.NewGuid(), "b", "b@example.com", "g-2");
        _users.Setup(r => r.GetByGoogleIdAsync("g-2")).ReturnsAsync(existing);

        var user = await _sut.RegisterAsync("b-New-Name", "new@example.com", "g-2");

        Assert.AreSame(existing, user);
        _users.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task RegisterAsync_ExistingEmailDifferentGoogleId_ReturnsExistingWithoutAdding()
    {
        var existing = new User(Guid.NewGuid(), "Sasha", "sasha@example.com", "g-old");
        _users.Setup(r => r.GetByGoogleIdAsync("g-new")).ReturnsAsync((User?)null);
        _users.Setup(r => r.GetByEmailAsync("sasha@example.com")).ReturnsAsync(existing);

        var user = await _sut.RegisterAsync("Masha", "sasha@example.com", "g-new");

        Assert.AreSame(existing, user);
        _users.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task RegisterAsync_ExistingEmailDifferentCase_ReturnsExistingWithoutAdding()
    {
        var existing = new User(Guid.NewGuid(), "Dan", "dan@example.com", "g-old");
        _users.Setup(r => r.GetByGoogleIdAsync("g-new")).ReturnsAsync((User?)null);
        _users.Setup(r => r.GetByEmailAsync("dan@example.com")).ReturnsAsync(existing);

        var user = await _sut.RegisterAsync("Egor", "Dan@Example.com", "g-new");

        Assert.AreSame(existing, user);
        _users.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [DataTestMethod]
    [DataRow("", "e@e.com", "g")]
    [DataRow("  ", "e@e.com", "g")]
    [DataRow("a", "", "g")]
    [DataRow("a", "  ", "g")]
    [DataRow("a", "e@e.com", "")]
    [DataRow("a", "e@e.com", "  ")]
    public async Task RegisterAsync_InvalidArguments_Throws(string name, string email, string googleId)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.RegisterAsync(name, email, googleId));

        _users.Verify(r => r.GetByGoogleIdAsync(It.IsAny<string>()), Times.Never);
        _users.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "a", "a@e.com", "g-1");
        _users.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(id);

        Assert.AreSame(user, result);
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFound_ThrowsUserNotFoundException()
    {
        var id = Guid.NewGuid();
        _users.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((User?)null);

        await Assert.ThrowsExceptionAsync<UserNotFoundException>(
            () => _sut.GetByIdAsync(id));
    }

    [TestMethod]
    public async Task GetByIdAsync_EmptyId_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.GetByIdAsync(Guid.Empty));

        _users.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task FindByGoogleIdAsync_Existing_ReturnsUser()
    {
        var user = new User(Guid.NewGuid(), "a", "a@e.com", "g-1");
        _users.Setup(r => r.GetByGoogleIdAsync("g-1")).ReturnsAsync(user);

        var result = await _sut.FindByGoogleIdAsync("g-1");

        Assert.AreSame(user, result);
    }

    [TestMethod]
    public async Task FindByGoogleIdAsync_NotFound_ReturnsNull()
    {
        _users.Setup(r => r.GetByGoogleIdAsync("g-x")).ReturnsAsync((User?)null);

        var result = await _sut.FindByGoogleIdAsync("g-x");

        Assert.IsNull(result);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task FindByGoogleIdAsync_InvalidGoogleId_Throws(string googleId)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.FindByGoogleIdAsync(googleId));

        _users.Verify(r => r.GetByGoogleIdAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task FindByEmailAsync_Existing_ReturnsUser()
    {
        var user = new User(Guid.NewGuid(), "Egor", "egor@example.com", "g-1");
        _users.Setup(r => r.GetByEmailAsync("egor@example.com")).ReturnsAsync(user);

        var result = await _sut.FindByEmailAsync("Egor@Example.com");

        Assert.AreSame(user, result);
    }

    [TestMethod]
    public async Task FindByEmailAsync_NotFound_ReturnsNull()
    {
        _users.Setup(r => r.GetByEmailAsync("missing@example.com")).ReturnsAsync((User?)null);

        var result = await _sut.FindByEmailAsync("missing@example.com");

        Assert.IsNull(result);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task FindByEmailAsync_InvalidEmail_Throws(string email)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.FindByEmailAsync(email));

        _users.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }
}

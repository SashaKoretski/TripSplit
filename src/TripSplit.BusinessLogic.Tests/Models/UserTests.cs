using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests.Models;

/// <summary>
/// Классический (без mock/stub) unit-тест доменной модели User.
/// Техника данных: классы эквивалентности (пустая/из пробелов строка) для каждого из
/// трех обязательных строковых полей.
/// </summary>
[TestClass]
public class UserTests
{
    [TestMethod]
    public void Ctor_ValidArguments_SetsAllProperties()
    {
        var id = Guid.NewGuid();

        var user = new User(id, "Sasha", "sasha@example.test", "gid-sasha");

        Assert.AreEqual(id, user.Id);
        Assert.AreEqual("Sasha", user.Name);
        Assert.AreEqual("sasha@example.test", user.Email);
        Assert.AreEqual("gid-sasha", user.GoogleId);
    }

    [TestMethod]
    public void Ctor_EmptyId_ThrowsArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new User(Guid.Empty, "Sasha", "a@e.com", "gid"));

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankName_ThrowsArgumentException(string name) =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new User(Guid.NewGuid(), name, "a@e.com", "gid"));

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankEmail_ThrowsArgumentException(string email) =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new User(Guid.NewGuid(), "Sasha", email, "gid"));

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankGoogleId_ThrowsArgumentException(string googleId) =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new User(Guid.NewGuid(), "Sasha", "a@e.com", googleId));

    [TestMethod]
    public void UserBuilder_WithOverrides_BuildsExpectedUser()
    {
        var user = new UserBuilder().WithName("Pasha").WithEmail("pasha@example.test").Build();

        Assert.AreEqual("Pasha", user.Name);
        Assert.AreEqual("pasha@example.test", user.Email);
    }
}

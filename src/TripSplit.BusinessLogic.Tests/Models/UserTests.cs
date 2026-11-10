using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests.Models;

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
    public void Ctor_EmptyId_ThrowsArgumentException()
    {
        var act = () => { _ = new User(Guid.Empty, "Sasha", "a@e.com", "gid"); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankName_ThrowsArgumentException(string name)
    {
        var act = () => { _ = new User(Guid.NewGuid(), name, "a@e.com", "gid"); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankEmail_ThrowsArgumentException(string email)
    {
        var act = () => { _ = new User(Guid.NewGuid(), "Sasha", email, "gid"); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankGoogleId_ThrowsArgumentException(string googleId)
    {
        var act = () => { _ = new User(Guid.NewGuid(), "Sasha", "a@e.com", googleId); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    public void UserBuilder_WithOverrides_BuildsExpectedUser()
    {
        var user = new UserBuilder().WithName("Pasha").WithEmail("pasha@example.test").Build();

        Assert.AreEqual("Pasha", user.Name);
        Assert.AreEqual("pasha@example.test", user.Email);
    }
}

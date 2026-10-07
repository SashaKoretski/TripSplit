using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests;

/// <summary>
/// Классический (Detroit-school) вариант тех же сценариев, что и в UserServiceTests.cs
/// (Лондонский вариант на Moq) — специально для сравнения двух стилей (ЛР1 Т3).
/// Разница: здесь зависимость — реальный (хоть и in-memory) InMemoryUserRepository,
/// проверяется итоговое СОСТОЯНИЕ репозитория, а не факт вызова метода (Verify).
/// </summary>
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

    // Классический аналог UserServiceTests.RegisterAsync_NewUser_CreatesAndPersists:
    // там Verify(AddAsync, Times.Once); здесь — реально читаем сохраненное состояние обратно.
    [TestMethod]
    public async Task RegisterAsync_NewUser_CreatesAndPersists()
    {
        var user = await _sut.RegisterAsync("Dasha", "dasha@example.test", "gid-dasha");

        var persisted = await _users.GetByIdAsync(user.Id);
        Assert.IsNotNull(persisted);
        Assert.AreEqual("Dasha", persisted!.Name);
        Assert.AreEqual(1, _users.AddCallCount);
    }

    // Классический аналог UserServiceTests.RegisterAsync_ExistingGoogleId_ReturnsExistingWithoutAdding.
    [TestMethod]
    public async Task RegisterAsync_ExistingGoogleId_ReturnsExistingWithoutAdding()
    {
        var existing = await _sut.RegisterAsync("Dasha", "dasha@example.test", "gid-dasha");

        var result = await _sut.RegisterAsync("New Name", "new@example.test", "gid-dasha");

        Assert.AreEqual(existing.Id, result.Id);
        Assert.AreEqual(1, _users.AddCallCount);
    }

    // Классический аналог UserServiceTests.GetByIdAsync_NotFound_ThrowsUserNotFoundException.
    [TestMethod]
    public async Task GetByIdAsync_NotFound_ThrowsUserNotFoundException() =>
        await Assert.ThrowsExactlyAsync<UserNotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
}

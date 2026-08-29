using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Services;

namespace TripSplit.BusinessLogic.Tests;

[TestClass]
public class GreedyDebtMinimizationStrategyTests
{
    private GreedyDebtMinimizationStrategy _sut = null!;

    [TestInitialize]

    public void Setup() => _sut = new GreedyDebtMinimizationStrategy( new DebtSettlementOptions { MinTransferAmount = 0.01m });

    [TestMethod]
    public void Minimize_Null_Throws() =>
        Assert.ThrowsException<ArgumentNullException>(() => _sut.Minimize(null!));

    [TestMethod]
    public void Minimize_Empty_ReturnsEmpty()
    {
        var result = _sut.Minimize(new Dictionary<Guid, decimal>());

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Minimize_AllZeroBalances_ReturnsEmpty()
    {
        var balances = new Dictionary<Guid, decimal>
        {
            [Guid.NewGuid()] = 0m,
            [Guid.NewGuid()] = 0m,
        };

        var result = _sut.Minimize(balances);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Minimize_UnbalancedSum_Throws()
    {
        var balances = new Dictionary<Guid, decimal>
        {
            [Guid.NewGuid()] = -100m,
            [Guid.NewGuid()] = 50m,
        };

        Assert.ThrowsException<ArgumentException>(() => _sut.Minimize(balances));
    }

    [TestMethod]
    public void Minimize_TinyImbalanceWithinEpsilon_Accepted()
    {
        var balances = new Dictionary<Guid, decimal>
        {
            [Guid.NewGuid()] = -100.00m,
            [Guid.NewGuid()] = 99.995m,
        };

        var result = _sut.Minimize(balances);

        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Minimize_TwoPeople_OneTransfer()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var balances = new Dictionary<Guid, decimal>
        {
            [a] = -100m,
            [b] = 100m,
        };

        var result = _sut.Minimize(balances);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(a, result[0].FromUserId);
        Assert.AreEqual(b, result[0].ToUserId);
        Assert.AreEqual(100m, result[0].Amount);
    }

    [TestMethod]
    public void Minimize_OneDebtorTwoCreditors_TwoTransfers()
    {
        var debtor = Guid.NewGuid();
        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        var balances = new Dictionary<Guid, decimal>
        {
            [debtor] = -300m,
            [c1] = 200m,
            [c2] = 100m,
        };

        var result = _sut.Minimize(balances);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(t => t.FromUserId == debtor));
        Assert.AreEqual(300m, result.Sum(t => t.Amount));
        Assert.AreEqual(c1, result[0].ToUserId);
        Assert.AreEqual(200m, result[0].Amount);
    }

    [TestMethod]
    public void Minimize_TwoDebtorsOneCreditor_TwoTransfers()
    {
        var d1 = Guid.NewGuid();
        var d2 = Guid.NewGuid();
        var creditor = Guid.NewGuid();
        var balances = new Dictionary<Guid, decimal>
        {
            [d1] = -200m,
            [d2] = -100m,
            [creditor] = 300m,
        };

        var result = _sut.Minimize(balances);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(t => t.ToUserId == creditor));
        Assert.AreEqual(300m, result.Sum(t => t.Amount));
    }

    [TestMethod]
    public void Minimize_MultipleParties_BalancesZeroOut()
    {
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var balances = new Dictionary<Guid, decimal>
        {
            [ids[0]] = -50m,
            [ids[1]] = -30m,
            [ids[2]] = 20m,
            [ids[3]] = 60m,
        };

        var result = _sut.Minimize(balances);

        var final = balances.ToDictionary(kv => kv.Key, kv => kv.Value);
        foreach (var t in result)
        {
            final[t.FromUserId] += t.Amount;
            final[t.ToUserId] -= t.Amount;
        }
        foreach (var b in final.Values)
            Assert.IsTrue(Math.Abs(b) < 0.01m, $"Balance not zero: {b}");
    }
}

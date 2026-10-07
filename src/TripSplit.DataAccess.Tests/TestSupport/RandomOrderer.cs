using Xunit.Abstractions;
using Xunit.Sdk;

// Применяет случайный порядок тестов внутри класса ко всей сборке (Т10 LAB01).
[assembly: Xunit.TestCaseOrderer(
    "TripSplit.DataAccess.Tests.TestSupport.RandomOrderer", "TripSplit.DataAccess.Tests")]

namespace TripSplit.DataAccess.Tests.TestSupport;

/// <summary>
/// Запускает тесты внутри каждого класса в случайном порядке (Т10 LAB01).
/// Seed воспроизводим: берется из переменной окружения TRIPSPLIT_TEST_SEED,
/// если задана, иначе генерируется и печатается в stderr, чтобы прогон можно было повторить.
/// </summary>
public sealed class RandomOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        var seed = ResolveSeed();
        var rnd = new Random(seed);
        var list = testCases.ToList();
        Console.Error.WriteLine($"[RandomOrderer] seed={seed} (set TRIPSPLIT_TEST_SEED to reproduce)");

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private static int ResolveSeed()
    {
        var env = Environment.GetEnvironmentVariable("TRIPSPLIT_TEST_SEED");
        return int.TryParse(env, out var seed) ? seed : Environment.TickCount;
    }
}

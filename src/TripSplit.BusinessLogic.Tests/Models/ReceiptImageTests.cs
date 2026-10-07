using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Tests.Models;

/// <summary>
/// Классический unit-тест доменной модели ReceiptImage.
/// Техника данных: граничное значение размера файла (0 — невалидно, 1 байт — валидно).
/// </summary>
[TestClass]
public class ReceiptImageTests
{
    private static ReceiptImage Valid(long size = 1024) => new(
        Guid.NewGuid(), Guid.NewGuid(), "receipts/t/r/x.jpg", "image/jpeg", size, "check.jpg", DateTime.UtcNow);

    [TestMethod]
    public void Ctor_ValidArguments_SetsAllProperties()
    {
        var image = Valid(2048);

        Assert.AreEqual("image/jpeg", image.ContentType);
        Assert.AreEqual(2048, image.FileSizeBytes);
        Assert.AreEqual("check.jpg", image.OriginalFileName);
    }

    [TestMethod]
    public void Ctor_EmptyId_ThrowsArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new ReceiptImage(Guid.Empty, Guid.NewGuid(), "k", "image/jpeg", 1, "f.jpg", DateTime.UtcNow));

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    public void Ctor_NonPositiveFileSize_ThrowsArgumentException(long size) =>
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new ReceiptImage(Guid.NewGuid(), Guid.NewGuid(), "k", "image/jpeg", size, "f.jpg", DateTime.UtcNow));

    [TestMethod]
    public void Ctor_SmallestPositiveFileSize_IsAccepted()
    {
        var image = Valid(1);

        Assert.AreEqual(1, image.FileSizeBytes);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankStorageKey_ThrowsArgumentException(string key) =>
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new ReceiptImage(Guid.NewGuid(), Guid.NewGuid(), key, "image/jpeg", 1, "f.jpg", DateTime.UtcNow));
}

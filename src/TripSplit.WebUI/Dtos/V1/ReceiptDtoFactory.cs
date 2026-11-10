using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Dtos.V1;

// Общий способ собрать ReceiptDto (с изображением, если есть) - переиспользуется
// между listTripReceipts и getReceiptById, чтобы не дублировать сборку url/urlExpiresAt.
public static class ReceiptDtoFactory
{
    public static async Task<ReceiptDto> BuildAsync(Receipt receipt, IReceiptImageService images)
    {
        var image = await images.GetByReceiptAsync(receipt.Id);
        if (image is null)
            return ReceiptDto.FromDomain(receipt, null);

        var url = await images.GetDownloadUrlAsync(receipt.Id);
        var imageDto = ReceiptImageDto.FromDomain(image, url, DateTime.UtcNow.Add(images.DownloadUrlLifetime));
        return ReceiptDto.FromDomain(receipt, imageDto);
    }
}

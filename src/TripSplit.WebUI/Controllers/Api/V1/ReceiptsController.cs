using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.WebUI.Dtos.V1;

namespace TripSplit.WebUI.Controllers.Api.V1;

[Route("api/v1/receipts")]
public class ReceiptsController : ApiControllerBase
{
    // как и в MVC ReceiptsController.MaxUploadBytes / ReceiptImageOptions.MaxFileSizeBytes -
    // два независимо заданных лимита, сейчас совпадают (docs/api/review.md, Этап 7 LAB02)
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private readonly IReceiptService _receipts;
    private readonly IReceiptImageService _receiptImages;
    private readonly ITripService _trips;

    public ReceiptsController(IReceiptService receipts, IReceiptImageService receiptImages, ITripService trips)
    {
        _receipts = receipts;
        _receiptImages = receiptImages;
        _trips = trips;
    }

    // operationId: getReceiptById
    [HttpGet("{receiptId:guid}")]
    public async Task<ActionResult<ReceiptDto>> GetById(Guid receiptId)
    {
        var receipt = await _receipts.GetByIdAsync(receiptId);
        var trip = await _trips.GetByIdAsync(receipt.TripId);
        EnsureParticipant(trip);

        return Ok(await ReceiptDtoFactory.BuildAsync(receipt, _receiptImages));
    }

    // operationId: uploadReceiptImage - идемпотентная полная замена (ReceiptImageService.UploadAsync
    // сама удаляет предыдущее изображение перед сохранением нового)
    [HttpPut("{receiptId:guid}/image")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<ReceiptImageDto>> UploadImage(Guid receiptId, IFormFile file)
    {
        var receipt = await _receipts.GetByIdAsync(receiptId);
        var trip = await _trips.GetByIdAsync(receipt.TripId);
        EnsureParticipant(trip);

        await using var stream = file.OpenReadStream();
        var image = await _receiptImages.UploadAsync(receiptId, stream, file.FileName, file.ContentType, file.Length);

        var url = await _receiptImages.GetDownloadUrlAsync(receiptId);
        var dto = ReceiptImageDto.FromDomain(image, url, DateTime.UtcNow.Add(_receiptImages.DownloadUrlLifetime));
        return Ok(dto);
    }

    // operationId: getReceiptImage - вариант "файл" (302 на presigned S3 URL), для <img src>
    [HttpGet("{receiptId:guid}/image")]
    public async Task<IActionResult> GetImage(Guid receiptId)
    {
        var receipt = await _receipts.GetByIdAsync(receiptId);
        var trip = await _trips.GetByIdAsync(receipt.TripId);
        EnsureParticipant(trip);

        var url = await _receiptImages.GetDownloadUrlAsync(receiptId);
        return Redirect(url);
    }

    // operationId: deleteReceipt - физическое удаление (нет deleted_at у receipts), каскадно
    // удаляет изображение. Намеренно не проверяет статус поездки - так уже устроено в
    // ReceiptService.DeleteAsync (асимметрия с createReceipt, зафиксирована в docs/api/review.md,
    // бизнес-правило не меняем).
    [HttpDelete("{receiptId:guid}")]
    public async Task<IActionResult> Delete(Guid receiptId)
    {
        var receipt = await _receipts.GetByIdAsync(receiptId);
        var trip = await _trips.GetByIdAsync(receipt.TripId);
        EnsureParticipant(trip);

        await _receipts.DeleteAsync(receiptId);
        return NoContent();
    }

    // operationId: deleteReceiptImage
    [HttpDelete("{receiptId:guid}/image")]
    public async Task<IActionResult> DeleteImage(Guid receiptId)
    {
        var receipt = await _receipts.GetByIdAsync(receiptId);
        var trip = await _trips.GetByIdAsync(receipt.TripId);
        EnsureParticipant(trip);

        _ = await _receiptImages.GetByReceiptAsync(receiptId)
            ?? throw new ReceiptImageNotFoundException(receiptId);

        await _receiptImages.DeleteByReceiptAsync(receiptId);
        return NoContent();
    }
}

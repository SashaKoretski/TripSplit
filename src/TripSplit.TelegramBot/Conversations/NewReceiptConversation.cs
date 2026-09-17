namespace TripSplit.TelegramBot.Conversations;

// Мастер добавления чека: название -> фото (или /skip); дата всегда сегодняшняя
public sealed class NewReceiptConversation : ConversationBase
{
    private enum Step { Name, Photo }

    private readonly IReceiptService _receipts;
    private readonly IReceiptImageService _images;
    private readonly ITripService _trips;
    private readonly Guid _tripId;

    private Step _step = Step.Name;
    private Guid? _receiptId;

    public NewReceiptConversation(IReceiptService receipts, IReceiptImageService images, ITripService trips, Guid tripId)
    {
        _receipts = receipts;
        _images = images;
        _trips = trips;
        _tripId = tripId;
    }

    public override async Task StartAsync(ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct)
    {
        var trip = await _trips.GetByIdAsync(_tripId);
        if (trip.Status == TripStatus.Finished)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Поездка уже завершена, добавлять чеки нельзя.", cancellationToken: ct);
            session.ActiveConversation = null;
            return;
        }

        await bot.SendTextMessageAsync(message.Chat.Id, "Название чека? (/cancel — отменить)", cancellationToken: ct);
    }

    public override async Task<ConversationStepResult> HandleMessageAsync(
        ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct)
    {
        if (_step == Step.Name)
        {
            var text = message.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                await bot.SendTextMessageAsync(message.Chat.Id, "Пришлите название текстом.", cancellationToken: ct);
                return ConversationStepResult.Continue;
            }

            var receipt = await _receipts.CreateAsync(_tripId, text, DateOnly.FromDateTime(DateTime.UtcNow));
            _receiptId = receipt.Id;
            _step = Step.Photo;
            await bot.SendTextMessageAsync(message.Chat.Id, "Пришлите фото чека (или /skip).", cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        // Step.Photo
        if (message.Photo is { Length: > 0 } photos)
        {
            await SavePhotoAsync(bot, message.Chat.Id, photos, ct);
            return ConversationStepResult.Completed;
        }

        if (string.Equals(message.Text?.Trim(), "/skip", StringComparison.OrdinalIgnoreCase))
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Чек создан.", cancellationToken: ct);
            return ConversationStepResult.Completed;
        }

        await bot.SendTextMessageAsync(message.Chat.Id, "Пришлите фото чека или /skip.", cancellationToken: ct);
        return ConversationStepResult.Continue;
    }

    private async Task SavePhotoAsync(ITelegramBotClient bot, ChatId chatId, PhotoSize[] photos, CancellationToken ct)
    {
        var best = photos.OrderByDescending(p => p.Width * p.Height).First();
        var file = await bot.GetFileAsync(best.FileId, ct);

        using var stream = new MemoryStream();
        await bot.DownloadFileAsync(file.FilePath!, stream, ct);
        stream.Position = 0;

        await _images.UploadAsync(_receiptId!.Value, stream, $"telegram_{best.FileId}.jpg", "image/jpeg", stream.Length);
        await bot.SendTextMessageAsync(chatId, "Чек создан.", cancellationToken: ct);
    }
}

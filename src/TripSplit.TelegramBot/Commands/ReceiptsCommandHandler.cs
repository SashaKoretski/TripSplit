namespace TripSplit.TelegramBot.Commands;

public sealed class ReceiptsCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;
    private readonly IReceiptService _receipts;

    public ReceiptsCommandHandler(ITripService trips, IReceiptService receipts)
    {
        _trips = trips;
        _receipts = receipts;
    }

    public string Command => "/receipts";

    public async Task HandleAsync(
        ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        if (session.CurrentTripId is not { } tripId)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Сначала выберите поездку: /trips", cancellationToken: ct);
            return;
        }

        var receipts = await _receipts.GetByTripAsync(tripId);
        if (receipts.Count == 0)
        {
            await bot.SendTextMessageAsync(
                message.Chat.Id, "В этой поездке пока нет чеков. Добавьте: /newreceipt", cancellationToken: ct);
            return;
        }

        var rows = receipts.Select(r => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"{r.Name} ({r.Date:dd.MM.yyyy})", CallbackPrefixes.ViewReceipt + r.Id.ToString("N"))
        });

        await bot.SendTextMessageAsync(
            message.Chat.Id, "Чеки:", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
    }
}

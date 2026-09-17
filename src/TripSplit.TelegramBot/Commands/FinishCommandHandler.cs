namespace TripSplit.TelegramBot.Commands;

public sealed class FinishCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;

    public FinishCommandHandler(ITripService trips)
    {
        _trips = trips;
    }

    public string Command => "/finish";

    public async Task HandleAsync(
        ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        if (session.CurrentTripId is not { } tripId)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Сначала выберите поездку: /trips", cancellationToken: ct);
            return;
        }

        var trip = await _trips.GetByIdAsync(tripId);
        if (trip.Status == TripStatus.Finished)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Поездка уже завершена.", cancellationToken: ct);
            return;
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Да, завершить", CallbackPrefixes.FinishTrip + trip.Id.ToString("N")),
            InlineKeyboardButton.WithCallbackData("Отмена", CallbackPrefixes.Cancel)
        });

        await bot.SendTextMessageAsync(
            message.Chat.Id,
            $"Завершить поездку «{trip.Name}»? После этого добавлять траты и чеки будет нельзя.",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}

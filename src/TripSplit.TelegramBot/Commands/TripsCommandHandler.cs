namespace TripSplit.TelegramBot.Commands;

public sealed class TripsCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;

    public TripsCommandHandler(ITripService trips)
    {
        _trips = trips;
    }

    public string Command => "/trips";

    public async Task HandleAsync(
        ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        var trips = await _trips.GetByUserAsync(session.UserId);
        if (trips.Count == 0)
        {
            await bot.SendTextMessageAsync(
                message.Chat.Id,
                "У вас пока нет поездок. Создайте: /newtrip — либо перейдите по ссылке-приглашению.",
                cancellationToken: ct);
            return;
        }

        var rows = trips.Select(t => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"{(t.Id == session.CurrentTripId ? "• " : "")}{t.Name} [{(t.Status == TripStatus.Finished ? "завершена" : "активна")}]",
                CallbackPrefixes.SelectTrip + t.Id.ToString("N"))
        });

        await bot.SendTextMessageAsync(
            message.Chat.Id, "Ваши поездки:", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
    }
}

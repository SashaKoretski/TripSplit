using TripSplit.TelegramBot.Configuration;

namespace TripSplit.TelegramBot.Commands;

public sealed class InviteCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;
    private readonly TelegramOptions _options;

    public InviteCommandHandler(ITripService trips, TelegramOptions options)
    {
        _trips = trips;
        _options = options;
    }

    public string Command => "/invite";

    public async Task HandleAsync(
        ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        if (session.CurrentTripId is not { } tripId)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Сначала выберите поездку: /trips", cancellationToken: ct);
            return;
        }

        var trip = await _trips.GetByIdAsync(tripId);
        var link = $"https://t.me/{_options.BotUsername}?start={trip.Id.ToString("N")}";

        await bot.SendTextMessageAsync(
            message.Chat.Id,
            $"Перешлите эту ссылку тому, кого хотите позвать в «{trip.Name}»:\n{link}",
            cancellationToken: ct);
    }
}

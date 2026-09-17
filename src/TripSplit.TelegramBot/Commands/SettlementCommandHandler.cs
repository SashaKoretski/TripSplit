namespace TripSplit.TelegramBot.Commands;

public sealed class SettlementCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;
    private readonly IDebtSettlementService _settlement;
    private readonly IUserService _users;

    public SettlementCommandHandler(ITripService trips, IDebtSettlementService settlement, IUserService users)
    {
        _trips = trips;
        _settlement = settlement;
        _users = users;
    }

    public string Command => "/settlement";

    public async Task HandleAsync(
        ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        if (session.CurrentTripId is not { } tripId)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Сначала выберите поездку: /trips", cancellationToken: ct);
            return;
        }

        var trip = await _trips.GetByIdAsync(tripId);
        var stats = await _settlement.CalculateSettlementAsync(tripId);
        var text = await SettlementFormatter.BuildAsync(trip, stats, _users);

        await bot.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: ct);
    }
}

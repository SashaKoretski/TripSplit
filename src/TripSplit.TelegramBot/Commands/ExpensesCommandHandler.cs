using System.Text;

namespace TripSplit.TelegramBot.Commands;

public sealed class ExpensesCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;
    private readonly IExpenseService _expenses;
    private readonly IUserService _users;

    public ExpensesCommandHandler(ITripService trips, IExpenseService expenses, IUserService users)
    {
        _trips = trips;
        _expenses = expenses;
        _users = users;
    }

    public string Command => "/expenses";

    public async Task HandleAsync(
        ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        if (session.CurrentTripId is not { } tripId)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Сначала выберите поездку: /trips", cancellationToken: ct);
            return;
        }

        var trip = await _trips.GetByIdAsync(tripId);
        var expenses = await _expenses.GetByTripAsync(tripId);
        if (expenses.Count == 0)
        {
            await bot.SendTextMessageAsync(
                message.Chat.Id, "В этой поездке пока нет трат. Добавьте: /addexpense", cancellationToken: ct);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Траты «{trip.Name}»:");
        foreach (var e in expenses)
        {
            var payer = await _users.GetByIdAsync(e.PayerId);
            sb.AppendLine($"• {e.Name} — {e.EffectiveAmount} {trip.Currency} (заплатил {payer.Name}, {e.Type})");
        }

        await bot.SendTextMessageAsync(message.Chat.Id, sb.ToString(), cancellationToken: ct);
    }
}

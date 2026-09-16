using TripSplit.TelegramBot.Notifications;

namespace TripSplit.TelegramBot.Commands;

public sealed class AddExpenseCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;
    private readonly IExpenseService _expenses;
    private readonly IUserService _users;
    private readonly ITripNotifier _notifier;

    public AddExpenseCommandHandler(ITripService trips, IExpenseService expenses, IUserService users, ITripNotifier notifier)
    {
        _trips = trips;
        _expenses = expenses;
        _users = users;
        _notifier = notifier;
    }

    public string Command => "/addexpense";

    public Task HandleAsync(ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        if (session.CurrentTripId is not { } tripId)
            return bot.SendTextMessageAsync(message.Chat.Id, "Сначала выберите поездку: /trips", cancellationToken: ct);

        var conversation = new AddExpenseConversation(_trips, _expenses, _users, _notifier, tripId);
        session.ActiveConversation = conversation;
        return conversation.StartAsync(bot, message, session, ct);
    }
}

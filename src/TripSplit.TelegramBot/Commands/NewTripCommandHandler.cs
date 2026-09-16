namespace TripSplit.TelegramBot.Commands;

public sealed class NewTripCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;

    public NewTripCommandHandler(ITripService trips)
    {
        _trips = trips;
    }

    public string Command => "/newtrip";

    public Task HandleAsync(ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        var conversation = new NewTripConversation(_trips);
        session.ActiveConversation = conversation;
        return conversation.StartAsync(bot, message, session, ct);
    }
}

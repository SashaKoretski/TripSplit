namespace TripSplit.TelegramBot.Commands;

public sealed class NewReceiptCommandHandler : ICommandHandler
{
    private readonly IReceiptService _receipts;
    private readonly IReceiptImageService _images;
    private readonly ITripService _trips;

    public NewReceiptCommandHandler(IReceiptService receipts, IReceiptImageService images, ITripService trips)
    {
        _receipts = receipts;
        _images = images;
        _trips = trips;
    }

    public string Command => "/newreceipt";

    public Task HandleAsync(ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        if (session.CurrentTripId is not { } tripId)
            return bot.SendTextMessageAsync(message.Chat.Id, "Сначала выберите поездку: /trips", cancellationToken: ct);

        var conversation = new NewReceiptConversation(_receipts, _images, _trips, tripId);
        session.ActiveConversation = conversation;
        return conversation.StartAsync(bot, message, session, ct);
    }
}

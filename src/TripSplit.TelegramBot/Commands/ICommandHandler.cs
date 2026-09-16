namespace TripSplit.TelegramBot.Commands;

// Обработчик одной текстовой команды бота (например, "/trips")
public interface ICommandHandler
{
    string Command { get; }

    Task HandleAsync(ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct);
}

namespace TripSplit.TelegramBot.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    public string Command => "/help";

    public Task HandleAsync(ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct) =>
        bot.SendTextMessageAsync(
            message.Chat.Id,
            "Команды:\n" +
            "/newtrip — создать поездку\n" +
            "/trips — список поездок, выбрать текущую\n" +
            "/invite — пригласить в текущую поездку\n" +
            "/addexpense — добавить трату\n" +
            "/expenses — список трат текущей поездки\n" +
            "/newreceipt — добавить чек\n" +
            "/receipts — чеки текущей поездки\n" +
            "/settlement — кто кому должен\n" +
            "/finish — завершить поездку\n" +
            "/cancel — отменить текущий диалог",
            cancellationToken: ct);
}

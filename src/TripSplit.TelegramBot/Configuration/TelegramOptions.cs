namespace TripSplit.TelegramBot.Configuration;

// Настройки подключения к Telegram Bot API
public sealed class TelegramOptions
{
    public string BotToken { get; init; } = string.Empty;
    public string BotUsername { get; init; } = string.Empty;
}

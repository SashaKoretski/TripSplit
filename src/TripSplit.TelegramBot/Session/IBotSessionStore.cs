namespace TripSplit.TelegramBot.Session;

// Хранилище сессий, по одной на пользователя Telegram
public interface IBotSessionStore
{
    BotSession GetOrCreate(long telegramUserId);
}

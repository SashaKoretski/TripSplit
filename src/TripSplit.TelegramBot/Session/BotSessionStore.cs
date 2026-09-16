using System.Collections.Concurrent;

namespace TripSplit.TelegramBot.Session;

public sealed class BotSessionStore : IBotSessionStore
{
    private readonly ConcurrentDictionary<long, BotSession> _sessions = new();

    public BotSession GetOrCreate(long telegramUserId) =>
        _sessions.GetOrAdd(telegramUserId, _ => new BotSession());
}

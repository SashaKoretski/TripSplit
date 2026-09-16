using Microsoft.Extensions.Logging;

namespace TripSplit.TelegramBot.Notifications;

public sealed class TripNotifier : ITripNotifier
{
    private const string TelegramGoogleIdPrefix = "telegram:";

    private readonly IUserService _users;
    private readonly ILogger<TripNotifier> _logger;

    public TripNotifier(IUserService users, ILogger<TripNotifier> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task NotifyOthersAsync(ITelegramBotClient bot, Trip trip, Guid actingUserId, string message, CancellationToken ct)
    {
        foreach (var participantId in trip.ParticipantIds)
        {
            if (participantId == actingUserId) continue;

            var participant = await _users.GetByIdAsync(participantId);
            if (!TryGetTelegramChatId(participant.GoogleId, out var chatId)) continue;

            try
            {
                await bot.SendTextMessageAsync(chatId, message, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify user {UserId} about trip {TripId}", participantId, trip.Id);
            }
        }
    }

    // Пользователи бота регистрируются с GoogleId вида "telegram:<id>" — из него восстанавливаем chat id
    private static bool TryGetTelegramChatId(string googleId, out long chatId)
    {
        chatId = 0;
        return googleId.StartsWith(TelegramGoogleIdPrefix)
            && long.TryParse(googleId[TelegramGoogleIdPrefix.Length..], out chatId);
    }
}

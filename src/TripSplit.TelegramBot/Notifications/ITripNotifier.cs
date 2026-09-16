namespace TripSplit.TelegramBot.Notifications;

// Рассылает короткое уведомление остальным участникам поездки (кроме автора действия)
public interface ITripNotifier
{
    Task NotifyOthersAsync(ITelegramBotClient bot, Trip trip, Guid actingUserId, string message, CancellationToken ct);
}

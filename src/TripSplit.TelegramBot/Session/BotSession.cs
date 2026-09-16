namespace TripSplit.TelegramBot.Session;

// Состояние общения с одним пользователем Telegram между обновлениями (in-memory, без персистентности)
public sealed class BotSession
{
    public Guid UserId { get; set; }
    public Guid? CurrentTripId { get; set; }
    public Guid? ViewingReceiptId { get; set; }
    public IConversation? ActiveConversation { get; set; }
}

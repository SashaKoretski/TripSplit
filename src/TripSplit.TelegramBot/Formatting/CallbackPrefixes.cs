namespace TripSplit.TelegramBot.Formatting;

// Префиксы callback_data для кнопок, не привязанных к активному диалогу-мастеру
public static class CallbackPrefixes
{
    public const string Join = "join:";
    public const string SelectTrip = "select:";
    public const string FinishTrip = "finish:";
    public const string ViewReceipt = "receiptview:";
    public const string AttachToReceipt = "receiptattach:";
    public const string DetachFromReceipt = "receiptdetach:";
    public const string Cancel = "cancel";
}

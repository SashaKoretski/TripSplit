namespace TripSplit.TelegramBot.Conversations;

// Мастер создания поездки: название -> валюта
public sealed class NewTripConversation : ConversationBase
{
    private enum Step { Name, Currency }

    private readonly ITripService _trips;
    private Step _step = Step.Name;
    private string? _name;

    public NewTripConversation(ITripService trips)
    {
        _trips = trips;
    }

    public override Task StartAsync(ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct) =>
        bot.SendTextMessageAsync(message.Chat.Id, "Название новой поездки? (/cancel — отменить)", cancellationToken: ct);

    public override async Task<ConversationStepResult> HandleMessageAsync(
        ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct)
    {
        var text = message.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Пришлите текстом.", cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        if (_step == Step.Name)
        {
            _name = text;
            _step = Step.Currency;
            await bot.SendTextMessageAsync(
                message.Chat.Id, "Валюта (3 латинские буквы, например RUB, USD, EUR):", cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        var currency = text.ToUpperInvariant();
        if (currency.Length != 3 || !currency.All(char.IsLetter))
        {
            await bot.SendTextMessageAsync(
                message.Chat.Id, "Валюта должна состоять из 3 латинских букв, повторите.", cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        var trip = await _trips.CreateAsync(_name!, currency, session.UserId);
        session.CurrentTripId = trip.Id;
        await bot.SendTextMessageAsync(
            message.Chat.Id,
            $"Поездка «{trip.Name}» создана и выбрана текущей.\nПригласите участников: /invite",
            cancellationToken: ct);
        return ConversationStepResult.Completed;
    }
}

namespace TripSplit.TelegramBot.Conversations;

// Результат обработки одного шага диалога
public enum ConversationStepResult
{
    NotHandled,
    Continue,
    Completed
}

// Многошаговый диалог (мастер) с одним пользователем; хранит прогресс в полях своего экземпляра
public interface IConversation
{
    Task StartAsync(ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct);

    Task<ConversationStepResult> HandleMessageAsync(
        ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct);

    Task<ConversationStepResult> HandleCallbackAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, CancellationToken ct);
}

public abstract class ConversationBase : IConversation
{
    public abstract Task StartAsync(ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct);

    public virtual Task<ConversationStepResult> HandleMessageAsync(
        ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct) =>
        Task.FromResult(ConversationStepResult.NotHandled);

    public virtual Task<ConversationStepResult> HandleCallbackAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, CancellationToken ct) =>
        Task.FromResult(ConversationStepResult.NotHandled);
}

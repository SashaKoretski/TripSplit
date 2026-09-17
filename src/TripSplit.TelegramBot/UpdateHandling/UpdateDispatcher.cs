using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Polling;
using TripSplit.TelegramBot.Commands;
using TripSplit.TelegramBot.Identity;
using TripSplit.TelegramBot.Infrastructure;
using TripSplit.TelegramBot.Notifications;

namespace TripSplit.TelegramBot.UpdateHandling;

// Единая точка входа для всех обновлений от Telegram: маршрутизация команд, диалогов-мастеров и inline-кнопок
public sealed class UpdateDispatcher : IUpdateHandler
{
    private readonly IReadOnlyDictionary<string, ICommandHandler> _commands;
    private readonly ITelegramUserResolver _userResolver;
    private readonly IBotSessionStore _sessions;
    private readonly ITripService _trips;
    private readonly IExpenseService _expenses;
    private readonly IReceiptService _receipts;
    private readonly IReceiptImageService _receiptImages;
    private readonly IDebtSettlementService _settlement;
    private readonly IUserService _users;
    private readonly ITripNotifier _notifier;
    private readonly ILogger<UpdateDispatcher> _logger;

    public UpdateDispatcher(
        IEnumerable<ICommandHandler> commands,
        ITelegramUserResolver userResolver,
        IBotSessionStore sessions,
        ITripService trips,
        IExpenseService expenses,
        IReceiptService receipts,
        IReceiptImageService receiptImages,
        IDebtSettlementService settlement,
        IUserService users,
        ITripNotifier notifier,
        ILogger<UpdateDispatcher> logger)
    {
        _commands = commands.ToDictionary(c => c.Command, StringComparer.OrdinalIgnoreCase);
        _userResolver = userResolver;
        _sessions = sessions;
        _trips = trips;
        _expenses = expenses;
        _receipts = receipts;
        _receiptImages = receiptImages;
        _settlement = settlement;
        _users = users;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Message is { } message)
                await HandleMessageAsync(bot, message, ct);
            else if (update.CallbackQuery is { } query)
                await HandleCallbackAsync(bot, query, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle update {UpdateId}", update.Id);
            var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
            if (chatId is { } id)
                await bot.SendTextMessageAsync(id, DomainErrorMessages.Resolve(ex), cancellationToken: ct);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Polling error");
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        if (message.Chat.Type != ChatType.Private) return;
        if (message.From is null || message.From.IsBot) return;

        var user = await _userResolver.ResolveAsync(message.From);
        var session = _sessions.GetOrCreate(message.From.Id);
        session.UserId = user.Id;

        var text = message.Text?.Trim();

        if (string.Equals(text, "/cancel", StringComparison.OrdinalIgnoreCase))
        {
            var hadActive = session.ActiveConversation is not null;
            session.ActiveConversation = null;
            await bot.SendTextMessageAsync(
                message.Chat.Id, hadActive ? "Диалог отменен." : "Нет активного диалога.", cancellationToken: ct);
            return;
        }

        if (session.ActiveConversation is { } conversation)
        {
            var result = await conversation.HandleMessageAsync(bot, message, session, ct);
            if (result == ConversationStepResult.Completed)
                session.ActiveConversation = null;
            return;
        }

        if (text is { Length: > 0 } && text.StartsWith('/'))
        {
            var (command, argument) = ParseCommand(text);
            if (_commands.TryGetValue(command, out var handler))
                await handler.HandleAsync(bot, message, session, argument, ct);
            else
                await bot.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда. /help — список команд.", cancellationToken: ct);
            return;
        }

        await bot.SendTextMessageAsync(message.Chat.Id, "Не понял. /help — список команд.", cancellationToken: ct);
    }

    private async Task HandleCallbackAsync(ITelegramBotClient bot, CallbackQuery query, CancellationToken ct)
    {
        if (query.From.IsBot) return;

        var user = await _userResolver.ResolveAsync(query.From);
        var session = _sessions.GetOrCreate(query.From.Id);
        session.UserId = user.Id;

        if (session.ActiveConversation is { } conversation)
        {
            var result = await conversation.HandleCallbackAsync(bot, query, session, ct);
            if (result == ConversationStepResult.Completed)
                session.ActiveConversation = null;
            if (result != ConversationStepResult.NotHandled)
                return;
        }

        await HandleGlobalCallbackAsync(bot, query, session, ct);
    }

    private async Task HandleGlobalCallbackAsync(ITelegramBotClient bot, CallbackQuery query, BotSession session, CancellationToken ct)
    {
        var data = query.Data ?? string.Empty;
        var chatId = query.Message?.Chat.Id;

        if (data == CallbackPrefixes.Cancel)
        {
            await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
            if (chatId is { } cid && query.Message is { } msg)
                await bot.EditMessageTextAsync(cid, msg.MessageId, "Отменено.", cancellationToken: ct);
            return;
        }

        if (chatId is not { } targetChatId)
        {
            await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
            return;
        }

        if (data.StartsWith(CallbackPrefixes.Join))
        {
            await HandleJoinAsync(bot, query, session, data[CallbackPrefixes.Join.Length..], targetChatId, ct);
            return;
        }

        if (data.StartsWith(CallbackPrefixes.SelectTrip))
        {
            await HandleSelectTripAsync(bot, query, session, data[CallbackPrefixes.SelectTrip.Length..], targetChatId, ct);
            return;
        }

        if (data.StartsWith(CallbackPrefixes.FinishTrip))
        {
            await HandleFinishAsync(bot, query, session, data[CallbackPrefixes.FinishTrip.Length..], targetChatId, ct);
            return;
        }

        if (data.StartsWith(CallbackPrefixes.ViewReceipt))
        {
            await HandleViewReceiptAsync(bot, query, session, data[CallbackPrefixes.ViewReceipt.Length..], targetChatId, ct);
            return;
        }

        if (data.StartsWith(CallbackPrefixes.AttachToReceipt))
        {
            await HandleAttachToReceiptAsync(bot, query, session, data[CallbackPrefixes.AttachToReceipt.Length..], targetChatId, ct);
            return;
        }

        if (data.StartsWith(CallbackPrefixes.DetachFromReceipt))
        {
            await HandleDetachFromReceiptAsync(bot, query, session, data[CallbackPrefixes.DetachFromReceipt.Length..], targetChatId, ct);
            return;
        }

        await bot.AnswerCallbackQueryAsync(query.Id, "Кнопка устарела.", cancellationToken: ct);
    }

    private async Task HandleJoinAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, string tripIdHex, ChatId chatId, CancellationToken ct)
    {
        if (!Guid.TryParseExact(tripIdHex, "N", out var tripId))
        {
            await bot.AnswerCallbackQueryAsync(query.Id, "Некорректная ссылка.", cancellationToken: ct);
            return;
        }

        await _trips.AddParticipantAsync(tripId, session.UserId);
        var trip = await _trips.GetByIdAsync(tripId);
        session.CurrentTripId = trip.Id;

        await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
        await bot.EditMessageTextAsync(chatId, query.Message!.MessageId, $"Вы присоединились к поездке «{trip.Name}».", cancellationToken: ct);

        var actor = await _users.GetByIdAsync(session.UserId);
        await _notifier.NotifyOthersAsync(bot, trip, session.UserId, $"{actor.Name} присоединился к поездке «{trip.Name}».", ct);
    }

    private async Task HandleSelectTripAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, string tripIdHex, ChatId chatId, CancellationToken ct)
    {
        if (!Guid.TryParseExact(tripIdHex, "N", out var tripId))
        {
            await bot.AnswerCallbackQueryAsync(query.Id, "Некорректный выбор.", cancellationToken: ct);
            return;
        }

        var trip = await _trips.GetByIdAsync(tripId);
        if (!trip.ParticipantIds.Contains(session.UserId))
        {
            await bot.AnswerCallbackQueryAsync(query.Id, "Вы не участник этой поездки.", showAlert: true, cancellationToken: ct);
            return;
        }

        session.CurrentTripId = trip.Id;
        await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
        await bot.EditMessageTextAsync(
            chatId, query.Message!.MessageId,
            $"Текущая поездка: «{trip.Name}» ({(trip.Status == TripStatus.Finished ? "завершена" : "активна")}).",
            cancellationToken: ct);
    }

    private async Task HandleFinishAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, string tripIdHex, ChatId chatId, CancellationToken ct)
    {
        if (!Guid.TryParseExact(tripIdHex, "N", out var tripId))
        {
            await bot.AnswerCallbackQueryAsync(query.Id, "Некорректный выбор.", cancellationToken: ct);
            return;
        }

        await _trips.FinishAsync(tripId);
        var trip = await _trips.GetByIdAsync(tripId);
        var stats = await _settlement.CalculateSettlementAsync(tripId);
        var summary = await SettlementFormatter.BuildAsync(trip, stats, _users);

        await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
        await bot.EditMessageTextAsync(chatId, query.Message!.MessageId, $"Поездка «{trip.Name}» завершена.", cancellationToken: ct);
        await bot.SendTextMessageAsync(chatId, summary, cancellationToken: ct);

        var actor = await _users.GetByIdAsync(session.UserId);
        await _notifier.NotifyOthersAsync(bot, trip, session.UserId, $"{actor.Name} завершил поездку «{trip.Name}».\n\n{summary}", ct);
    }

    private async Task HandleViewReceiptAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, string receiptIdHex, ChatId chatId, CancellationToken ct)
    {
        if (!Guid.TryParseExact(receiptIdHex, "N", out var receiptId))
        {
            await bot.AnswerCallbackQueryAsync(query.Id, "Некорректный выбор.", cancellationToken: ct);
            return;
        }

        var receipt = await _receipts.GetByIdAsync(receiptId);
        session.ViewingReceiptId = receipt.Id;

        var (text, keyboard) = await BuildReceiptViewAsync(receipt);
        await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
        await bot.EditMessageTextAsync(chatId, query.Message!.MessageId, text, replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task HandleAttachToReceiptAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, string expenseIdHex, ChatId chatId, CancellationToken ct)
    {
        if (!Guid.TryParseExact(expenseIdHex, "N", out var expenseId) || session.ViewingReceiptId is not { } receiptId)
        {
            await bot.AnswerCallbackQueryAsync(query.Id, "Откройте чек заново: /receipts", cancellationToken: ct);
            return;
        }

        await _expenses.AttachToReceiptAsync(expenseId, receiptId);
        var receipt = await _receipts.GetByIdAsync(receiptId);

        var (text, keyboard) = await BuildReceiptViewAsync(receipt);
        await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
        await bot.EditMessageTextAsync(chatId, query.Message!.MessageId, text, replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task HandleDetachFromReceiptAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, string expenseIdHex, ChatId chatId, CancellationToken ct)
    {
        if (!Guid.TryParseExact(expenseIdHex, "N", out var expenseId))
        {
            await bot.AnswerCallbackQueryAsync(query.Id, "Некорректный выбор.", cancellationToken: ct);
            return;
        }

        await _expenses.DetachFromReceiptAsync(expenseId);

        if (session.ViewingReceiptId is not { } receiptId)
        {
            await bot.AnswerCallbackQueryAsync(query.Id, "Трата откреплена.", cancellationToken: ct);
            return;
        }

        var receipt = await _receipts.GetByIdAsync(receiptId);
        var (text, keyboard) = await BuildReceiptViewAsync(receipt);
        await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
        await bot.EditMessageTextAsync(chatId, query.Message!.MessageId, text, replyMarkup: keyboard, cancellationToken: ct);
    }

    // Текст и клавиатура карточки чека: сумма и привязанные/непривязанные траты для управления одним нажатием
    private async Task<(string Text, InlineKeyboardMarkup Keyboard)> BuildReceiptViewAsync(Receipt receipt)
    {
        var tripExpenses = await _expenses.GetByTripAsync(receipt.TripId);
        var linked = tripExpenses.Where(e => e.ReceiptId == receipt.Id).ToList();
        var unlinked = tripExpenses.Where(e => e.ReceiptId is null).ToList();
        var hasImage = await _receiptImages.GetByReceiptAsync(receipt.Id) is not null;

        var sb = new StringBuilder();
        sb.AppendLine($"Чек «{receipt.Name}» от {receipt.Date:dd.MM.yyyy}{(hasImage ? "" : " (без фото)")}");
        sb.AppendLine();
        if (linked.Count == 0)
        {
            sb.AppendLine("Трат пока не привязано.");
        }
        else
        {
            sb.AppendLine("Привязанные траты:");
            foreach (var e in linked)
                sb.AppendLine($"• {e.Name} — {e.EffectiveAmount}");
        }

        var rows = new List<IEnumerable<InlineKeyboardButton>>();
        foreach (var e in linked)
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"✖ Открепить: {e.Name}", CallbackPrefixes.DetachFromReceipt + e.Id.ToString("N"))
            });
        foreach (var e in unlinked)
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"+ Привязать: {e.Name}", CallbackPrefixes.AttachToReceipt + e.Id.ToString("N"))
            });

        return (sb.ToString(), new InlineKeyboardMarkup(rows));
    }

    private static (string Command, string? Argument) ParseCommand(string text)
    {
        var spaceIdx = text.IndexOf(' ');
        var raw = spaceIdx < 0 ? text : text[..spaceIdx];
        var atIdx = raw.IndexOf('@');
        var command = (atIdx < 0 ? raw : raw[..atIdx]).ToLowerInvariant();
        var argument = spaceIdx < 0 ? null : text[(spaceIdx + 1)..].Trim();
        return (command, string.IsNullOrEmpty(argument) ? null : argument);
    }
}

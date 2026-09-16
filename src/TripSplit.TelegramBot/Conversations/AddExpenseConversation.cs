using TripSplit.TelegramBot.Notifications;

namespace TripSplit.TelegramBot.Conversations;

// Мастер добавления траты: название -> тип -> сумма -> скидка -> плательщик -> потребители
public sealed class AddExpenseConversation : ConversationBase
{
    private enum Step { Name, Type, Value, DiscountChoice, DiscountAmount, DiscountPercent, Payer, Consumers }

    private readonly ITripService _trips;
    private readonly IExpenseService _expenses;
    private readonly IUserService _users;
    private readonly ITripNotifier _notifier;
    private readonly Guid _tripId;

    private Step _step = Step.Name;
    private string? _name;
    private ExpenseType _type;
    private decimal _value;
    private decimal _discount;
    private Guid _payerId;
    private readonly HashSet<Guid> _consumerIds = new();
    private List<TripSplit.BusinessLogic.Models.User> _participants = new();
    private Trip? _trip;

    public AddExpenseConversation(
        ITripService trips, IExpenseService expenses, IUserService users, ITripNotifier notifier, Guid tripId)
    {
        _trips = trips;
        _expenses = expenses;
        _users = users;
        _notifier = notifier;
        _tripId = tripId;
    }

    public override async Task StartAsync(ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct)
    {
        _trip = await _trips.GetByIdAsync(_tripId);
        if (_trip.Status == TripStatus.Finished)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Поездка уже завершена, добавлять траты нельзя.", cancellationToken: ct);
            session.ActiveConversation = null;
            return;
        }

        _participants = new List<TripSplit.BusinessLogic.Models.User>();
        foreach (var id in _trip.ParticipantIds)
            _participants.Add(await _users.GetByIdAsync(id));

        await bot.SendTextMessageAsync(message.Chat.Id, "Название траты? (/cancel — отменить)", cancellationToken: ct);
    }

    public override async Task<ConversationStepResult> HandleMessageAsync(
        ITelegramBotClient bot, Message message, BotSession session, CancellationToken ct)
    {
        if (_step == Step.Name)
        {
            var text = message.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                await bot.SendTextMessageAsync(message.Chat.Id, "Пришлите название текстом.", cancellationToken: ct);
                return ConversationStepResult.Continue;
            }
            _name = text;
            _step = Step.Type;
            await bot.SendTextMessageAsync(message.Chat.Id, "Тип траты:", replyMarkup: BuildTypeKeyboard(), cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        if (_step == Step.Value)
        {
            if (!TryParseAmount(message.Text, out var value) || value <= 0)
            {
                await bot.SendTextMessageAsync(message.Chat.Id, "Введите положительное число, например 1250.50", cancellationToken: ct);
                return ConversationStepResult.Continue;
            }
            _value = value;
            _step = Step.DiscountChoice;
            await bot.SendTextMessageAsync(message.Chat.Id, "Скидка?", replyMarkup: BuildDiscountChoiceKeyboard(), cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        if (_step == Step.DiscountAmount)
        {
            if (!TryParseAmount(message.Text, out var amount) || amount < 0 || amount > _value)
            {
                await bot.SendTextMessageAsync(message.Chat.Id, $"Введите число от 0 до {_value}.", cancellationToken: ct);
                return ConversationStepResult.Continue;
            }
            _discount = amount;
            return await MoveToPayerAsync(bot, message.Chat.Id, ct);
        }

        if (_step == Step.DiscountPercent)
        {
            if (!TryParseAmount(message.Text, out var percent) || percent < 0 || percent > 100)
            {
                await bot.SendTextMessageAsync(message.Chat.Id, "Введите процент от 0 до 100.", cancellationToken: ct);
                return ConversationStepResult.Continue;
            }
            _discount = Math.Round(_value * percent / 100m, 2, MidpointRounding.AwayFromZero);
            return await MoveToPayerAsync(bot, message.Chat.Id, ct);
        }

        await bot.SendTextMessageAsync(message.Chat.Id, "Выберите вариант кнопкой выше (или /cancel).", cancellationToken: ct);
        return ConversationStepResult.Continue;
    }

    public override async Task<ConversationStepResult> HandleCallbackAsync(
        ITelegramBotClient bot, CallbackQuery query, BotSession session, CancellationToken ct)
    {
        var data = query.Data ?? string.Empty;
        var chatId = query.Message!.Chat.Id;

        if (_step == Step.Type && data.StartsWith("etype:"))
        {
            var idx = int.Parse(data["etype:".Length..]);
            _type = Enum.GetValues<ExpenseType>()[idx];
            _step = Step.Value;
            await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
            await bot.EditMessageTextAsync(chatId, query.Message.MessageId, $"Тип: {_type}", cancellationToken: ct);
            await bot.SendTextMessageAsync(chatId, "Сумма?", cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        if (_step == Step.DiscountChoice && data.StartsWith("discount:"))
        {
            var choice = data["discount:".Length..];
            await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);

            if (choice == "none")
            {
                _discount = 0;
                await bot.EditMessageTextAsync(chatId, query.Message.MessageId, "Без скидки.", cancellationToken: ct);
                return await MoveToPayerAsync(bot, chatId, ct);
            }
            if (choice == "amount")
            {
                _step = Step.DiscountAmount;
                await bot.EditMessageTextAsync(chatId, query.Message.MessageId, $"Сумма скидки (0..{_value}):", cancellationToken: ct);
                return ConversationStepResult.Continue;
            }
            if (choice == "percent")
            {
                _step = Step.DiscountPercent;
                await bot.EditMessageTextAsync(chatId, query.Message.MessageId, "Процент скидки (0-100):", cancellationToken: ct);
                return ConversationStepResult.Continue;
            }
            return ConversationStepResult.NotHandled;
        }

        if (_step == Step.Payer && data.StartsWith("payer:"))
        {
            var idx = int.Parse(data["payer:".Length..]);
            _payerId = _participants[idx].Id;
            _step = Step.Consumers;
            await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
            await bot.EditMessageTextAsync(chatId, query.Message.MessageId, $"Платит: {_participants[idx].Name}", cancellationToken: ct);
            await bot.SendTextMessageAsync(
                chatId, "На кого потрачено? Отметьте участников и нажмите «Готово».",
                replyMarkup: BuildConsumersKeyboard(), cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        if (_step == Step.Consumers && data.StartsWith("consumer:"))
        {
            var idx = int.Parse(data["consumer:".Length..]);
            var id = _participants[idx].Id;
            if (!_consumerIds.Remove(id)) _consumerIds.Add(id);

            await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
            await bot.EditMessageReplyMarkupAsync(chatId, query.Message.MessageId, BuildConsumersKeyboard(), cancellationToken: ct);
            return ConversationStepResult.Continue;
        }

        if (_step == Step.Consumers && data == "consumers:done")
        {
            if (_consumerIds.Count == 0)
            {
                await bot.AnswerCallbackQueryAsync(query.Id, "Отметьте хотя бы одного участника.", showAlert: true, cancellationToken: ct);
                return ConversationStepResult.Continue;
            }

            var expense = await _expenses.AddAsync(_tripId, _payerId, _name!, _type, _value, _discount, _consumerIds);
            await bot.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
            await bot.EditMessageTextAsync(
                chatId, query.Message.MessageId,
                $"Добавлено: «{expense.Name}» — {expense.EffectiveAmount} {_trip!.Currency}.",
                cancellationToken: ct);

            var actor = await _users.GetByIdAsync(session.UserId);
            await _notifier.NotifyOthersAsync(
                bot, _trip, session.UserId,
                $"{actor.Name} добавил трату «{expense.Name}» — {expense.EffectiveAmount} {_trip.Currency} в поездке «{_trip.Name}».",
                ct);

            return ConversationStepResult.Completed;
        }

        return ConversationStepResult.NotHandled;
    }

    private async Task<ConversationStepResult> MoveToPayerAsync(ITelegramBotClient bot, ChatId chatId, CancellationToken ct)
    {
        _step = Step.Payer;
        await bot.SendTextMessageAsync(chatId, "Кто платил?", replyMarkup: BuildParticipantsKeyboard("payer:"), cancellationToken: ct);
        return ConversationStepResult.Continue;
    }

    private static InlineKeyboardMarkup BuildTypeKeyboard()
    {
        var values = Enum.GetValues<ExpenseType>();
        var buttons = values.Select((v, i) => InlineKeyboardButton.WithCallbackData(v.ToString(), $"etype:{i}"));
        return new InlineKeyboardMarkup(buttons.Chunk(2));
    }

    private static InlineKeyboardMarkup BuildDiscountChoiceKeyboard() => new(new[]
    {
        InlineKeyboardButton.WithCallbackData("Без скидки", "discount:none"),
        InlineKeyboardButton.WithCallbackData("Сумма", "discount:amount"),
        InlineKeyboardButton.WithCallbackData("Процент", "discount:percent")
    });

    private InlineKeyboardMarkup BuildParticipantsKeyboard(string prefix)
    {
        var rows = _participants.Select((p, i) => new[] { InlineKeyboardButton.WithCallbackData(p.Name, $"{prefix}{i}") });
        return new InlineKeyboardMarkup(rows);
    }

    private InlineKeyboardMarkup BuildConsumersKeyboard()
    {
        var rows = _participants
            .Select((p, i) => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    (_consumerIds.Contains(p.Id) ? "☑ " : "⬜ ") + p.Name, $"consumer:{i}")
            })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Готово", "consumers:done") });
        return new InlineKeyboardMarkup(rows);
    }

    private static bool TryParseAmount(string? text, out decimal value) =>
        decimal.TryParse(
            text?.Trim().Replace(',', '.'),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out value);
}

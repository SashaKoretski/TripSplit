namespace TripSplit.TelegramBot.Commands;

// /start без аргумента — приветствие; /start <tripId> — переход по ссылке-приглашению
public sealed class StartCommandHandler : ICommandHandler
{
    private readonly ITripService _trips;

    public StartCommandHandler(ITripService trips)
    {
        _trips = trips;
    }

    public string Command => "/start";

    public async Task HandleAsync(
        ITelegramBotClient bot, Message message, BotSession session, string? argument, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            await bot.SendTextMessageAsync(
                message.Chat.Id,
                "Привет! Я TripSplit-бот — помогу считать общие траты в поездке.\n\n" +
                "/newtrip — создать поездку\n" +
                "/trips — список ваших поездок\n" +
                "/help — все команды",
                cancellationToken: ct);
            return;
        }

        if (!Guid.TryParseExact(argument, "N", out var tripId))
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Ссылка-приглашение повреждена.", cancellationToken: ct);
            return;
        }

        Trip trip;
        try
        {
            trip = await _trips.GetByIdAsync(tripId);
        }
        catch (TripNotFoundException)
        {
            await bot.SendTextMessageAsync(message.Chat.Id, "Поездка по этой ссылке не найдена.", cancellationToken: ct);
            return;
        }

        if (trip.ParticipantIds.Contains(session.UserId))
        {
            session.CurrentTripId = trip.Id;
            await bot.SendTextMessageAsync(
                message.Chat.Id, $"Вы уже участник поездки «{trip.Name}». Она выбрана текущей.", cancellationToken: ct);
            return;
        }

        if (trip.Status == TripStatus.Finished)
        {
            await bot.SendTextMessageAsync(
                message.Chat.Id, $"Поездка «{trip.Name}» уже завершена, присоединиться нельзя.", cancellationToken: ct);
            return;
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Присоединиться", CallbackPrefixes.Join + trip.Id.ToString("N")),
            InlineKeyboardButton.WithCallbackData("Отмена", CallbackPrefixes.Cancel)
        });

        await bot.SendTextMessageAsync(
            message.Chat.Id,
            $"Вас пригласили в поездку «{trip.Name}». Участников: {trip.ParticipantIds.Count}.\nПрисоединиться?",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}

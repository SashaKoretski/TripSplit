namespace TripSplit.TelegramBot.Identity;

// Резолвит доменного пользователя по учетной записи Telegram, регистрируя его при первом обращении
public interface ITelegramUserResolver
{
    Task<TripSplit.BusinessLogic.Models.User> ResolveAsync(Telegram.Bot.Types.User telegramUser);
}

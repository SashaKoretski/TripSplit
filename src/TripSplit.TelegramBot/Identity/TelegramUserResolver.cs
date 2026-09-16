namespace TripSplit.TelegramBot.Identity;

public sealed class TelegramUserResolver : ITelegramUserResolver
{
    private readonly IUserService _users;

    public TelegramUserResolver(IUserService users)
    {
        _users = users;
    }

    // Регистрация не нужна: пользователь молча заводится по своему Telegram id при первом сообщении боту
    public Task<TripSplit.BusinessLogic.Models.User> ResolveAsync(Telegram.Bot.Types.User telegramUser)
    {
        var googleId = $"telegram:{telegramUser.Id}";
        var email = $"telegram_{telegramUser.Id}@tripsplit.local";
        var name = BuildDisplayName(telegramUser);

        return _users.RegisterAsync(name, email, googleId);
    }

    private static string BuildDisplayName(Telegram.Bot.Types.User telegramUser)
    {
        var name = telegramUser.FirstName;
        if (!string.IsNullOrWhiteSpace(telegramUser.LastName))
            name += " " + telegramUser.LastName;
        if (!string.IsNullOrWhiteSpace(telegramUser.Username))
            name += $" (@{telegramUser.Username})";
        return name;
    }
}

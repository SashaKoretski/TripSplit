namespace TripSplit.ConsoleUI.Commands;

// эмуляция Google-логина. Настоящий OAuth будет во фронтенде.
public sealed class LoginCommand : IMenuItem
{
    private readonly IUserService _users;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public LoginCommand(IUserService users, IAppSession session, IConsoleIO io)
    {
        _users = users;
        _session = session;
        _io = io;
    }

    public string Title => _session.IsAuthenticated
        ? "Сменить пользователя"
        : "Войти (эмуляция Google)";

    public bool IsAvailable(IAppSession session) => true;

    public async Task ExecuteAsync()
    {
        var name = _io.ReadLine("Имя: ").Trim();
        var email = _io.ReadLine("Email: ").Trim();
        var googleId = _io.ReadLine("GoogleId (любая строка, эмуляция): ").Trim();

        var user = await _users.RegisterAsync(name, email, googleId);
        _session.CurrentUser = user;
        _session.CurrentTrip = null;

        _io.WriteLine($"Успех. {user.Name} [{user.Id}]");
    }
}

using Microsoft.Extensions.Logging;
using TripSplit.ConsoleUI.Session;

namespace TripSplit.ConsoleUI.Menu;

// Декоратор IMenuItem: пишет в лог действия пользователя и любые исключения.
public sealed class LoggingMenuItemDecorator : IMenuItem
{
    private readonly IMenuItem _inner;
    private readonly IAppSession _session;
    private readonly ILogger<LoggingMenuItemDecorator> _logger;

    public LoggingMenuItemDecorator(
        IMenuItem inner,
        IAppSession session,
        ILogger<LoggingMenuItemDecorator> logger)
    {
        _inner = inner;
        _session = session;
        _logger = logger;
    }

    public string Title => _inner.Title;

    public bool IsAvailable(IAppSession session) => _inner.IsAvailable(session);

    public async Task ExecuteAsync()
    {
        var user = _session.CurrentUser?.Name ?? "<anonymous>";
        var command = _inner.GetType().Name;

        _logger.LogInformation("User {User} executing {Command}", user, command);
        try
        {
            await _inner.ExecuteAsync().ConfigureAwait(false);
            _logger.LogInformation("User {User} completed {Command}", user, command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User {User} failed {Command}: {Message}", user, command, ex.Message);
            throw;
        }
    }
}
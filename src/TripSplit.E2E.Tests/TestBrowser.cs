using System.Net;
using System.Text.RegularExpressions;

namespace TripSplit.E2E.Tests;

// Минимальный HTTP-клиент, имитирующий браузер одного пользователя: своя cookie-банка
// (сессия + antiforgery-cookie), ручное следование редиректам и разбор HTML-форм ASP.NET Core
// (hidden __RequestVerificationToken, <option>/<input type=checkbox> для guid участников).
// Используется только E2E-тестом демо-сценария (ТИОПО ЛР2, З3/Т13) - без GUI, без Selenium.
public sealed class TestBrowser : IDisposable
{
    private readonly HttpClient _client;

    public TestBrowser(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = false
        };
        _client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<PageResult> GetAsync(string path)
    {
        using var response = await _client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        return new PageResult(response.StatusCode, response.Headers.Location, body);
    }

    // Принимает пары "имя поля - значение", а не Dictionary: форме можно отправить несколько
    // полей с одинаковым именем (как чекбоксы ConsumerIds в Views/Expenses/Create.cshtml).
    public async Task<PageResult> PostFormAsync(
        string path, string refererHtml, IEnumerable<KeyValuePair<string, string>> fields)
    {
        var token = ExtractAntiForgeryToken(refererHtml);
        var payload = new List<KeyValuePair<string, string>>(fields)
        {
            new("__RequestVerificationToken", token)
        };
        using var content = new FormUrlEncodedContent(payload);
        using var response = await _client.PostAsync(path, content);
        var body = await response.Content.ReadAsStringAsync();
        return new PageResult(response.StatusCode, response.Headers.Location, body);
    }

    public static string ExtractAntiForgeryToken(string html)
    {
        var tag = Regex.Match(html, "<input[^>]*__RequestVerificationToken[^>]*>", RegexOptions.IgnoreCase);
        if (!tag.Success)
            throw new InvalidOperationException("Antiforgery hidden field not found on the page");

        var value = Regex.Match(tag.Value, "value=\"([^\"]*)\"", RegexOptions.IgnoreCase);
        if (!value.Success)
            throw new InvalidOperationException("Antiforgery token has no value attribute");

        return value.Groups[1].Value;
    }

    // Находит guid из <option value="..."> или <input ... value="..."> рядом с именем участника,
    // как отрисовано в Views/Expenses/Create.cshtml (Payer select / ConsumerIds checkboxes)
    public static Guid FindParticipantId(string html, string participantName)
    {
        // [^>]* вокруг value="guid" - на <option>/<input> может быть добавлен произвольный
        // соседний атрибут (например, SelectTagHelper добавляет selected="selected" текущему
        // по умолчанию PayerId), порядок и набор атрибутов не являются частью контракта.
        var optionMatch = Regex.Match(
            html,
            $"<option[^>]*value=\"([0-9a-fA-F-]{{36}})\"[^>]*>{Regex.Escape(participantName)}</option>");
        if (optionMatch.Success)
            return Guid.Parse(optionMatch.Groups[1].Value);

        var checkboxMatch = Regex.Match(
            html,
            $"value=\"([0-9a-fA-F-]{{36}})\"[^>]*>\\s*<span>{Regex.Escape(participantName)}</span>");
        if (checkboxMatch.Success)
            return Guid.Parse(checkboxMatch.Groups[1].Value);

        throw new InvalidOperationException($"Participant '{participantName}' not found on the page");
    }

    public static Guid FindFirstGuidInPath(Uri? location)
    {
        if (location is null)
            throw new InvalidOperationException("Response has no Location header to extract an id from");

        var match = Regex.Match(location.ToString(), "([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})");
        if (!match.Success)
            throw new InvalidOperationException($"No guid found in redirect location '{location}'");

        return Guid.Parse(match.Value);
    }

    public void Dispose() => _client.Dispose();
}

public sealed record PageResult(HttpStatusCode StatusCode, Uri? Location, string Body);

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TripSplit.WebUI.Tests;

// Небольшие хелперы поверх HttpClient для интеграционных тестов - собирают типовые
// шаги сценариев (регистрация -> токен -> создание поездки), чтобы сами тесты
// читались как сценарий, а не как повтор одного и того же HTTP-боилерплейта.
public static class TestClient
{
    public static async Task<(Guid Id, string Email)> RegisterAsync(HttpClient client, string name, string email)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/users", new { name, email });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("id").GetGuid(), email);
    }

    public static async Task<string> GetTokenAsync(HttpClient client, string email)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new { email });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    public static async Task<(Guid UserId, string Token)> RegisterAndLoginAsync(HttpClient client, string name, string email)
    {
        var (id, _) = await RegisterAsync(client, name, email);
        var token = await GetTokenAsync(client, email);
        return (id, token);
    }

    public static void Authorize(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static async Task<JsonElement> CreateTripAsync(HttpClient client, string name = "Test Trip", string currency = "USD")
    {
        var resp = await client.PostAsJsonAsync("/api/v1/trips", new { name, currency });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task<JsonElement> CreateExpenseAsync(
        HttpClient client, Guid tripId, Guid payerId, Guid[] consumerIds,
        string name = "Expense", string type = "food", decimal value = 10m, decimal discount = 0m)
    {
        var resp = await client.PostAsJsonAsync($"/api/v1/trips/{tripId}/expenses", new
        {
            name, type, value, discount, payerId, consumerIds
        });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
    }
}

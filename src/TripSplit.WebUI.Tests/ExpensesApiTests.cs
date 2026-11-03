using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TripSplit.WebUI.Tests;

[Collection("Api")]
public class ExpensesApiTests
{
    private readonly ApiTestFixture _fixture;

    public ExpensesApiTests(ApiTestFixture fixture) => _fixture = fixture;

    private async Task<(HttpClient Client, Guid TripId, Guid UserId)> SetUpTripAsync()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();
        var (userId, token) = await TestClient.RegisterAndLoginAsync(client, "Anna", "anna@example.com");
        TestClient.Authorize(client, token);
        var trip = await TestClient.CreateTripAsync(client);
        return (client, trip.GetProperty("id").GetGuid(), userId);
    }

    [Fact]
    public async Task CreateExpense_Succeeds_WithCorrectEffectiveAmount()
    {
        var (client, tripId, userId) = await SetUpTripAsync();

        var expense = await TestClient.CreateExpenseAsync(client, tripId, userId, new[] { userId }, value: 60m, discount: 10m);

        Assert.Equal(50m, expense.GetProperty("effectiveAmount").GetDecimal());
    }

    [Fact]
    public async Task CreateExpense_DiscountExceedsValue_Returns422()
    {
        var (client, tripId, userId) = await SetUpTripAsync();

        var resp = await client.PostAsJsonAsync($"/api/v1/trips/{tripId}/expenses", new
        {
            name = "Bad", type = "food", value = 10m, discount = 20m, payerId = userId, consumerIds = new[] { userId }
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_expense", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PatchExpense_OmittedField_KeepsExistingValue()
    {
        var (client, tripId, userId) = await SetUpTripAsync();
        var expense = await TestClient.CreateExpenseAsync(client, tripId, userId, new[] { userId }, name: "Dinner", value: 40m);
        var expenseId = expense.GetProperty("id").GetGuid();

        var patchResp = await client.PatchAsJsonAsync($"/api/v1/expenses/{expenseId}", new { value = 55m });
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        var patched = await patchResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Dinner", patched.GetProperty("name").GetString());
        Assert.Equal(55m, patched.GetProperty("value").GetDecimal());
    }

    [Fact]
    public async Task PatchExpense_NullReceiptId_DetachesReceipt()
    {
        var (client, tripId, userId) = await SetUpTripAsync();
        var receiptResp = await client.PostAsJsonAsync($"/api/v1/trips/{tripId}/receipts", new { name = "R", date = "2026-05-01" });
        var receipt = await receiptResp.Content.ReadFromJsonAsync<JsonElement>();
        var receiptId = receipt.GetProperty("id").GetGuid();

        var expense = await TestClient.CreateExpenseAsync(client, tripId, userId, new[] { userId });
        var expenseId = expense.GetProperty("id").GetGuid();

        var attachResp = await client.PatchAsJsonAsync($"/api/v1/expenses/{expenseId}", new { receiptId });
        Assert.Equal(HttpStatusCode.OK, attachResp.StatusCode);

        var detachResp = await client.PatchAsJsonAsync($"/api/v1/expenses/{expenseId}", new { receiptId = (Guid?)null });
        Assert.Equal(HttpStatusCode.OK, detachResp.StatusCode);
        var detached = await detachResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, detached.GetProperty("receiptId").ValueKind);
    }

    [Fact]
    public async Task PatchExpense_NullName_Returns400()
    {
        var (client, tripId, userId) = await SetUpTripAsync();
        var expense = await TestClient.CreateExpenseAsync(client, tripId, userId, new[] { userId });
        var expenseId = expense.GetProperty("id").GetGuid();

        var resp = await client.PatchAsJsonAsync($"/api/v1/expenses/{expenseId}", new { name = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteExpense_Twice_SecondCallReturns404()
    {
        var (client, tripId, userId) = await SetUpTripAsync();
        var expense = await TestClient.CreateExpenseAsync(client, tripId, userId, new[] { userId });
        var expenseId = expense.GetProperty("id").GetGuid();

        var first = await client.DeleteAsync($"/api/v1/expenses/{expenseId}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.DeleteAsync($"/api/v1/expenses/{expenseId}");
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }
}

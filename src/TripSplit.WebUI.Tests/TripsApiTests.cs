using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TripSplit.WebUI.Tests;

[Collection("Api")]
public class TripsApiTests
{
    private readonly ApiTestFixture _fixture;

    public TripsApiTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateTrip_ThenGetById_Succeeds()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();
        var (_, token) = await TestClient.RegisterAndLoginAsync(client, "Anna", "anna@example.com");
        TestClient.Authorize(client, token);

        var created = await TestClient.CreateTripAsync(client, "Tbilisi", "gel");
        var tripId = created.GetProperty("id").GetGuid();

        Assert.Equal("active", created.GetProperty("status").GetString());
        Assert.Equal("GEL", created.GetProperty("currency").GetString());

        var getResp = await client.GetAsync($"/api/v1/trips/{tripId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    [Fact]
    public async Task CreateTrip_EmptyName_Returns400()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();
        var (_, token) = await TestClient.RegisterAndLoginAsync(client, "Anna", "anna@example.com");
        TestClient.Authorize(client, token);

        var resp = await client.PostAsJsonAsync("/api/v1/trips", new { name = "", currency = "USD" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetTripById_UnknownTrip_Returns404()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();
        var (_, token) = await TestClient.RegisterAndLoginAsync(client, "Anna", "anna@example.com");
        TestClient.Authorize(client, token);

        var resp = await client.GetAsync($"/api/v1/trips/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("trip_not_found", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetTripById_NotAParticipant_Returns403()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();

        var (_, ownerToken) = await TestClient.RegisterAndLoginAsync(client, "Anna", "anna@example.com");
        TestClient.Authorize(client, ownerToken);
        var trip = await TestClient.CreateTripAsync(client);
        var tripId = trip.GetProperty("id").GetGuid();

        var (_, outsiderToken) = await TestClient.RegisterAndLoginAsync(client, "Boris", "boris@example.com");
        TestClient.Authorize(client, outsiderToken);

        var resp = await client.GetAsync($"/api/v1/trips/{tripId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task FinishTrip_Twice_SecondCallReturns409()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();
        var (_, token) = await TestClient.RegisterAndLoginAsync(client, "Anna", "anna@example.com");
        TestClient.Authorize(client, token);
        var trip = await TestClient.CreateTripAsync(client);
        var tripId = trip.GetProperty("id").GetGuid();

        var first = await client.PostAsync($"/api/v1/trips/{tripId}/finish", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsync($"/api/v1/trips/{tripId}/finish", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("trip_already_finished", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AddParticipant_SelfJoin_IsIdempotent()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();
        var (_, ownerToken) = await TestClient.RegisterAndLoginAsync(client, "Anna", "anna@example.com");
        TestClient.Authorize(client, ownerToken);
        var trip = await TestClient.CreateTripAsync(client);
        var tripId = trip.GetProperty("id").GetGuid();

        // создатель уже первый участник (FR-004) - self-join нужен другому пользователю
        var (_, joinerToken) = await TestClient.RegisterAndLoginAsync(client, "Boris", "boris-join@example.com");
        TestClient.Authorize(client, joinerToken);

        var first = await client.PostAsJsonAsync($"/api/v1/trips/{tripId}/participants", new { });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync($"/api/v1/trips/{tripId}/participants", new { });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }
}

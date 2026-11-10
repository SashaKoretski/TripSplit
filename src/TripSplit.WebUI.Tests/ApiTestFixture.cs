using Xunit;

namespace TripSplit.WebUI.Tests;

public sealed class ApiTestFixture : IAsyncLifetime
{
    public ApiFactory Factory { get; } = new();

    public async Task InitializeAsync() => await Factory.InitializeSchemaAsync();

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }

    public Task ResetAsync() => Factory.ResetDataAsync();
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<ApiTestFixture> { }

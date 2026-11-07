using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using WireMock.Server;

namespace PixHook.Tests.Integration;

/// <summary>
/// Sobe a API completa em memória com um banco SQLite em arquivo temporário (migrations + seed reais)
/// e um WireMock simulando "a API do desenvolvedor" que recebe os webhooks.
/// </summary>
public sealed class PixHookApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"pixhook-tests-{Guid.NewGuid():N}.db");

    public WireMockServer Destination { get; } = WireMockServer.Start();

    public const string MercadoPagoSecret = "segredo-de-teste";
    public const int DispatchTimeoutSeconds = 2;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PixHook"] = $"Data Source={_dbPath}",
            ["Database:MigrateOnStartup"] = "true",
            ["Swagger:Enabled"] = "true",
            ["Dispatcher:TimeoutSeconds"] = DispatchTimeoutSeconds.ToString(),
            ["Dispatcher:RetryCount"] = "0",
            ["Signing:Secrets:mercadopago"] = MercadoPagoSecret,
            ["Serilog:WriteTo:1:Name"] = "Console"
        }));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        Destination.Stop();
        Destination.Dispose();
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch (IOException) { /* best effort */ }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<PixHookApiFactory>
{
    public const string Name = "api";
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PixHook.Tests.Integration;

public abstract class ApiTestBase
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected ApiTestBase(PixHookApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected PixHookApiFactory Factory { get; }
    protected HttpClient Client { get; }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recam.Server.Tests.Support;

public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static Task<HttpResponseMessage> PairAsync(this HttpClient client, string? token, string? name) =>
        client.PostAsJsonAsync(new Uri("/api/pair", UriKind.Relative), new { token, name }, Options);
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Recam.Server.Domain;

namespace Recam.Server.Tests.Support;

public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static readonly IReadOnlyList<DeviceRole> AnyRole = [DeviceRole.Owner, DeviceRole.Viewer, DeviceRole.Camera];

    public static Task<HttpResponseMessage> PairAsync(
        this HttpClient client, string? token, string? name, IReadOnlyList<DeviceRole>? expectedRoles = null) =>
        client.PostAsJsonAsync(
            new Uri("/api/pair", UriKind.Relative),
            new { token, name, expectedRoles = expectedRoles ?? AnyRole },
            Options);
}

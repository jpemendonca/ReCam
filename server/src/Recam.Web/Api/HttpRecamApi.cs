using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Recam.Web.Api;

/// <summary>
/// Talks to the server that served this page. The browser sends the device cookie on its own;
/// every request carries the X-Recam-Web header the server asks of cookie requests (SPECS.md 5.4).
/// </summary>
public sealed class HttpRecamApi(HttpClient http) : IRecamApi
{
    public const string WebHeader = "X-Recam-Web";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<MeInfo?> GetMeAsync(CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri("api/me", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeInfo>(Json, cancellationToken);
    }

    public async Task<bool> IsFirstOpenAsync(CancellationToken cancellationToken)
    {
        var status = await http.GetFromJsonAsync<FirstOpenStatus>(new Uri("api/web/first-open", UriKind.Relative), Json, cancellationToken);
        return status?.Open == true;
    }

    public async Task<FirstOpenOutcome> FirstOpenAsync(string code, bool remember, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(
            new Uri("api/web/first-open", UriKind.Relative), new { code, remember }, Json, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.NoContent => FirstOpenOutcome.Opened,
            HttpStatusCode.Unauthorized => FirstOpenOutcome.WrongCode,
            HttpStatusCode.Conflict => FirstOpenOutcome.AlreadyHasMonitor,
            HttpStatusCode.Forbidden => FirstOpenOutcome.NotLocal,
            HttpStatusCode.TooManyRequests => FirstOpenOutcome.TooManyAttempts,
            _ => throw new HttpRequestException($"Unexpected answer {(int)response.StatusCode} to the first-time code.", null, response.StatusCode),
        };
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        using var response = await http.PostAsync(new Uri("api/web/sign-out", UriKind.Relative), null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record FirstOpenStatus(bool Open);
}

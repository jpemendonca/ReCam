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

    public async Task<IReadOnlyList<CameraInfo>?> GetCamerasAsync(CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri("api/cameras", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CameraInfo>>(Json, cancellationToken) ?? [];
    }

    public async Task<PairingTokenInfo> CreatePairingTokenAsync(DeviceKind kind, CancellationToken cancellationToken)
    {
        var role = kind == DeviceKind.Camera ? "camera" : "viewer";
        using var response = await http.PostAsJsonAsync(new Uri("api/pairing-tokens", UriKind.Relative), new { role }, Json, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<CreatedToken>(Json, cancellationToken)
            ?? throw new HttpRequestException("The server answered an empty pairing token.");

        // Measured against the server's clock, which may differ from this computer's. The Date
        // header has whole seconds, so one second comes off to never promise more than there is.
        var validFor = response.Headers.Date is { } serverNow
            ? token.ExpiresAt - serverNow - TimeSpan.FromSeconds(1)
            : token.ExpiresAt - DateTimeOffset.UtcNow;
        return new PairingTokenInfo(token.Id, token.QrUri, validFor);
    }

    public async Task<bool> IsPairingTokenUsedAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var status = await http.GetFromJsonAsync<TokenStatus>(new Uri($"api/pairing-tokens/{tokenId}", UriKind.Relative), Json, cancellationToken);
        return status?.Used == true;
    }

    private sealed record FirstOpenStatus(bool Open);

    private sealed record CreatedToken(Guid Id, string QrUri, DateTimeOffset ExpiresAt);

    private sealed record TokenStatus(bool Used);
}

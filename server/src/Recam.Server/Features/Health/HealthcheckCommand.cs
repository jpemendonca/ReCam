using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Tls;

namespace Recam.Server.Features.Health;

/// <summary>
/// Container healthcheck: the aspnet image has no curl, so the server binary checks itself.
/// </summary>
public static class HealthcheckCommand
{
    public const string Argument = "healthcheck";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        var settings = ServerSettings.From(builder.Configuration, builder.Environment.ContentRootPath);

        using var expected = settings.TlsEnabled
            ? new CertificateStore(settings.DataDirectory, TimeProvider.System).Load()
            : null;
        if (settings.TlsEnabled && expected is null)
        {
            return 1;
        }

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                certificate is not null && expected is not null && expected.Matches(certificate),
        };
        using var client = new HttpClient(handler) { Timeout = Timeout };
        var scheme = settings.TlsEnabled ? "https" : "http";

        // Refused connections and timeouts mean "unhealthy"; the exit code is the only output.
        try
        {
            using var response = await client.GetAsync(
                new Uri($"{scheme}://localhost:{ServerSettings.HttpsPort}/health"), cancellationToken);
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (HttpRequestException)
        {
            return 1;
        }
        catch (TaskCanceledException)
        {
            return 1;
        }
    }
}

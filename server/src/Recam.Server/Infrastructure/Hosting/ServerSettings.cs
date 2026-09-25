using System.Net;

namespace Recam.Server.Infrastructure.Hosting;

public sealed record ServerSettings(string DataDirectory, IReadOnlyList<Uri> PublicUrls, string? Host)
{
    public const string DataDirectoryKey = "RECAM_DATA_DIR";
    public const string PublicUrlsKey = "RECAM_PUBLIC_URLS";
    public const string HostKey = "RECAM_HOST";
    public const string MediaMtxUrlKey = "RECAM_MEDIAMTX_URL";
    public const string RecordingsDirectoryKey = "RECAM_RECORDINGS_DIR";
    public const string TlsKey = "RECAM_TLS";
    public const string TrustedProxiesKey = "RECAM_TRUSTED_PROXIES";
    public const int HttpsPort = 8443;

    private const string DefaultDataDirectory = "/data";
    private const string DefaultRecordingsDirectory = "/recordings";

    /// <summary>MediaMTX WebRTC HTTP listener. Never exposed; only this server calls it.</summary>
    public Uri MediaMtxUrl { get; init; } = new("http://127.0.0.1:8889");

    /// <summary>Where MediaMTX writes recordings; the same volume is mounted in both containers.</summary>
    public string RecordingsDirectory { get; init; } = DefaultRecordingsDirectory;

    /// <summary>
    /// Off behind a reverse proxy that terminates TLS: the server speaks plain HTTP and the QR code
    /// carries no fingerprint, so the app checks the proxy's certificate with the system CAs.
    /// </summary>
    public bool TlsEnabled { get; init; } = true;

    /// <summary>Proxies whose X-Forwarded-* headers are believed. Empty: none are.</summary>
    public IReadOnlyList<IPNetwork> TrustedProxies { get; init; } = [];

    /// <summary>
    /// Reads operator configuration. Invalid values stop the server at startup, the same way
    /// options validation does, because nothing useful can run with a wrong address.
    /// </summary>
    public static ServerSettings From(IConfiguration configuration, string contentRootPath)
    {
        var dataDirectory = configuration[DataDirectoryKey] ?? DefaultDataDirectory;
        var host = configuration[HostKey] is { Length: > 0 } value ? value.Trim() : null;
        var settings = new ServerSettings(
            Path.GetFullPath(dataDirectory, contentRootPath),
            ParsePublicUrls(configuration[PublicUrlsKey]),
            host);
        if (configuration[RecordingsDirectoryKey] is { Length: > 0 } recordingsDirectory)
        {
            settings = settings with { RecordingsDirectory = Path.GetFullPath(recordingsDirectory, contentRootPath) };
        }

        settings = settings with
        {
            TlsEnabled = ParseTls(configuration[TlsKey]),
            TrustedProxies = ParseTrustedProxies(configuration[TrustedProxiesKey]),
        };

        if (configuration[MediaMtxUrlKey] is { Length: > 0 } mediaMtxUrl)
        {
            if (!Uri.TryCreate(mediaMtxUrl, UriKind.Absolute, out var url))
            {
                throw new InvalidOperationException($"{MediaMtxUrlKey} must be an absolute URL. Got '{mediaMtxUrl}'.");
            }

            settings = settings with { MediaMtxUrl = url };
        }

        return settings;
    }

    private static bool ParseTls(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "on" => true,
        "off" => false,
        _ => throw new InvalidOperationException($"{TlsKey} must be 'on' or 'off'. Got '{value}'."),
    };

    /// <summary>Addresses (<c>10.0.0.5</c>) or networks (<c>172.16.0.0/12</c>), comma separated.</summary>
    private static List<IPNetwork> ParseTrustedProxies(string? value)
    {
        var networks = new List<IPNetwork>();
        foreach (var part in (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (IPNetwork.TryParse(part, out var network))
            {
                networks.Add(network);
            }
            else if (IPAddress.TryParse(part, out var address))
            {
                networks.Add(new IPNetwork(address, address.GetAddressBytes().Length * 8));
            }
            else
            {
                throw new InvalidOperationException($"{TrustedProxiesKey} must list IP addresses or networks. Invalid entry: '{part}'.");
            }
        }

        return networks;
    }

    private static List<Uri> ParsePublicUrls(string? value)
    {
        var urls = new List<Uri>();
        foreach (var part in (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(part, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException($"{PublicUrlsKey} must contain absolute https URLs. Invalid entry: '{part}'.");
            }

            urls.Add(url);
        }

        return urls;
    }
}

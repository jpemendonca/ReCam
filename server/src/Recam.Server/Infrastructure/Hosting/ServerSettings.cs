namespace Recam.Server.Infrastructure.Hosting;

public sealed record ServerSettings(string DataDirectory, IReadOnlyList<Uri> PublicUrls, string? Host)
{
    public const string DataDirectoryKey = "RECAM_DATA_DIR";
    public const string PublicUrlsKey = "RECAM_PUBLIC_URLS";
    public const string HostKey = "RECAM_HOST";
    public const string MediaMtxUrlKey = "RECAM_MEDIAMTX_URL";
    public const int HttpsPort = 8443;

    private const string DefaultDataDirectory = "/data";

    /// <summary>MediaMTX WebRTC HTTP listener. Never exposed; only this server calls it.</summary>
    public Uri MediaMtxUrl { get; init; } = new("http://127.0.0.1:8889");

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

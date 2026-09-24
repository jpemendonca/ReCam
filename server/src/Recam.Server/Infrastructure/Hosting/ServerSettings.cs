namespace Recam.Server.Infrastructure.Hosting;

public sealed record ServerSettings(string DataDirectory, IReadOnlyList<Uri> PublicUrls, string? Host)
{
    public const string DataDirectoryKey = "RECAM_DATA_DIR";
    public const string PublicUrlsKey = "RECAM_PUBLIC_URLS";
    public const string HostKey = "RECAM_HOST";
    public const int HttpsPort = 8443;

    private const string DefaultDataDirectory = "/data";

    /// <summary>
    /// Reads operator configuration. Invalid values stop the server at startup, the same way
    /// options validation does, because nothing useful can run with a wrong address.
    /// </summary>
    public static ServerSettings From(IConfiguration configuration, string contentRootPath)
    {
        var dataDirectory = configuration[DataDirectoryKey] ?? DefaultDataDirectory;
        var host = configuration[HostKey] is { Length: > 0 } value ? value.Trim() : null;
        return new ServerSettings(
            Path.GetFullPath(dataDirectory, contentRootPath),
            ParsePublicUrls(configuration[PublicUrlsKey]),
            host);
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

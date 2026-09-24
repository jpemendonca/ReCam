namespace Recam.Server.Infrastructure.Hosting;

public sealed record ServerSettings(string DataDirectory)
{
    public const string DataDirectoryKey = "RECAM_DATA_DIR";
    public const int HttpsPort = 8443;

    private const string DefaultDataDirectory = "/data";

    public static ServerSettings From(IConfiguration configuration, string contentRootPath)
    {
        var dataDirectory = configuration[DataDirectoryKey] ?? DefaultDataDirectory;
        return new ServerSettings(Path.GetFullPath(dataDirectory, contentRootPath));
    }
}

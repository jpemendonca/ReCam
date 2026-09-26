namespace Recam.Server.Features.Setup;

/// <summary>
/// The current first-time code, kept in the data folder so <c>./Recam.Server code</c>, which runs
/// as another process, can show it. It exists only while the server has no Monitor. Like the log
/// line, it is not a credential: the code only makes the first Monitor.
/// </summary>
public static class FirstOpenCodeFile
{
    public const string Name = "first-open-code";

    public static Task WriteAsync(string dataDirectory, string code, CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(PathIn(dataDirectory), code, cancellationToken);

    public static void Delete(string dataDirectory) => File.Delete(PathIn(dataDirectory));

    /// <summary>The code, or null when the server has none.</summary>
    public static async Task<string?> ReadAsync(string dataDirectory, CancellationToken cancellationToken)
    {
        var path = PathIn(dataDirectory);
        return File.Exists(path) ? (await File.ReadAllTextAsync(path, cancellationToken)).Trim() : null;
    }

    private static string PathIn(string dataDirectory) => Path.Combine(dataDirectory, Name);
}

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Recam.Server.Tests.Support;

/// <summary>
/// A real MediaMTX, same image and configuration as production (deploy/), started once per
/// test class and thrown away afterwards.
/// </summary>
public sealed class MediaMtxFixture : IAsyncLifetime
{
    public const string Image = "bluenviron/mediamtx:1.21.1";
    private const ushort WebRtcHttpPort = 8889;

    private readonly IContainer _container = new ContainerBuilder(Image)
        .WithResourceMapping(new FileInfo(RepositoryFile("deploy/mediamtx.yml")), "/")
        .WithPortBinding(WebRtcHttpPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("WebRTC\\] started"))
        .Build();

    public Uri Url => new($"http://{_container.Hostname}:{_container.GetMappedPublicPort(WebRtcHttpPort)}");

    public static string RepositoryFile(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Could not find {relativePath} above the test output folder.");
    }

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

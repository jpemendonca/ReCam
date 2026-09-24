using System.Net;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Network;

namespace Recam.Server.Tests.Infrastructure.Network;

public sealed class PublicUrlResolverTests
{
    private static readonly IPAddress[] LocalAddresses =
    [
        IPAddress.Parse("192.168.0.10"),
        IPAddress.Parse("172.17.0.1"),
        IPAddress.Parse("8.8.8.8"),
        IPAddress.Parse("fe80::1"),
    ];

    [Fact(DisplayName = "Configured public URLs win over everything else")]
    public void Resolve_WithEnvVar_UsesEnvVarUrls()
    {
        // arrange
        var settings = new ServerSettings("/data", [new Uri("https://cam.example.com:8443")], "192.168.0.99");

        // act
        var urls = PublicUrlResolver.Resolve(settings, LocalAddresses);

        // assert
        Assert.Equal([new Uri("https://cam.example.com:8443")], urls);
    }

    [Fact(DisplayName = "RECAM_HOST becomes the only URL when no public URLs are set")]
    public void Resolve_WithHost_UsesHost()
    {
        // arrange
        var settings = new ServerSettings("/data", [], "192.168.0.99");

        // act
        var urls = PublicUrlResolver.Resolve(settings, LocalAddresses);

        // assert
        Assert.Equal([new Uri("https://192.168.0.99:8443")], urls);
    }

    [Fact(DisplayName = "Without configuration, private IPv4 addresses of the machine are used")]
    public void Resolve_WithoutConfiguration_UsesPrivateIPv4Addresses()
    {
        // arrange
        var settings = new ServerSettings("/data", [], null);

        // act
        var urls = PublicUrlResolver.Resolve(settings, LocalAddresses);

        // assert
        Assert.Equal([new Uri("https://192.168.0.10:8443"), new Uri("https://172.17.0.1:8443")], urls);
    }
}

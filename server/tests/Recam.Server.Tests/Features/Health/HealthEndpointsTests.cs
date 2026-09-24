using System.Net;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Health;

public sealed class HealthEndpointsTests(RecamApiFactory factory) : IClassFixture<RecamApiFactory>
{
    [Fact(DisplayName = "Health endpoint answers ok")]
    public async Task Health_WhenCalled_ReturnsOk()
    {
        // arrange
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", body);
    }

    [Fact(DisplayName = "Starting the server creates the TLS certificate in the data directory")]
    public void Startup_WithEmptyDataDirectory_CreatesCertificate()
    {
        // arrange
        var pfxPath = Path.Combine(factory.DataDirectory, "tls", "server.pfx");

        // act
        _ = factory.Services;

        // assert
        Assert.True(File.Exists(pfxPath));
    }
}

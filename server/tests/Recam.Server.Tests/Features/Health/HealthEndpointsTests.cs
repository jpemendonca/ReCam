using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Recam.Server.Tests.Features.Health;

public sealed class HealthEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
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
}

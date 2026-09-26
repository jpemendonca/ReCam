using System.Net;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Web;

public sealed class WebHostingTests
{
    [Fact(DisplayName = "The root serves the browser Monitor with a strict content security policy")]
    public async Task GetRoot_ServesAppWithSecurityHeaders()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("_framework/blazor.webassembly.js", html, StringComparison.Ordinal);
        var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", policy, StringComparison.Ordinal);
        Assert.Contains("script-src 'self' 'wasm-unsafe-eval';", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-inline", policy, StringComparison.Ordinal);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
    }

    [Fact(DisplayName = "A page of the browser Monitor is served by the app, whatever its path")]
    public async Task GetClientRoute_ServesApp()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(new Uri("/cameras/some-camera", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory(DisplayName = "An unknown server path is a 404, never the browser Monitor's page")]
    [InlineData("/api/nothing-here")]
    [InlineData("/hubs/nothing-here")]
    [InlineData("/whep/nothing-here/x")]
    public async Task GetUnknownServerPath_Returns404(string path)
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}

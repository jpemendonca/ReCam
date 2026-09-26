using Recam.Web.Pairing;

namespace Recam.Web.Tests.Pairing;

public sealed class QrSvgTests
{
    [Fact(DisplayName = "The QR is plain SVG without inline style, which the content security policy would block")]
    public void Render_NoStyleAttribute()
    {
        // arrange
        const string uri = "recam://pair?v=1&t=abc&r=camera&u=https%3A%2F%2F192.168.0.10%3A8443";

        // act
        var svg = QrSvg.Render(uri);

        // assert
        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("style", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<path fill=\"#000\" d=\"M", svg, StringComparison.Ordinal);
    }
}

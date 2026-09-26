using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Pages;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Pages;

public sealed class HomeTests : BunitContext
{
    public HomeTests() => Services.AddLocalization();

    [Theory(DisplayName = "The start page says this browser is not a Monitor yet, in the browser's language")]
    [InlineData("en-US", "This browser is not a Monitor yet")]
    [InlineData("pt-BR", "Este navegador ainda não é um Monitor")]
    public void Render_InBrowserLanguage_ShowsNotMonitorTitle(string language, string expected)
    {
        // arrange
        using var _ = Culture.Use(language);

        // act
        var page = Render<Home>();

        // assert
        Assert.Equal(expected, page.Find("h1").TextContent);
    }
}

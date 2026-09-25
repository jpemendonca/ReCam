using Microsoft.Net.Http.Headers;
using Recam.Server.Features.Setup;

namespace Recam.Server.Tests.Features.Setup;

public sealed class SetupTextsTests
{
    [Theory(DisplayName = "The browser's preferred language between Portuguese and English wins")]
    [InlineData("pt-BR,pt;q=0.9,en;q=0.8", "pt-BR")]
    [InlineData("en-US,en;q=0.9,pt;q=0.5", "en")]
    [InlineData("fr-FR,pt;q=0.7,en;q=0.9", "en")]
    [InlineData("fr-FR,de;q=0.9", "en")]
    [InlineData("", "en")]
    public void For_AcceptLanguage_PicksLanguage(string header, string expected)
    {
        // arrange
        var languages = StringWithQualityHeaderValue.ParseList(header.Split(',', StringSplitOptions.RemoveEmptyEntries));

        // act
        var texts = SetupTexts.For(languages);

        // assert
        Assert.Equal(expected, texts.Language);
    }
}

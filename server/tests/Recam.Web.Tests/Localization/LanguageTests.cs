using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Localization;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Localization;

public sealed class LanguageTests : BunitContext
{
    private readonly FakeLanguageStore _store = new();

    public LanguageTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<ILanguageStore>(_store);
    }

    [Theory(DisplayName = "A saved choice picks the culture, and no choice keeps the browser's")]
    [InlineData("pt", "pt-BR")]
    [InlineData("en", "en-US")]
    [InlineData("", null)]
    [InlineData("fr", null)]
    public void CultureFor_SavedChoice_PicksCulture(string saved, string? expected)
    {
        // act
        var culture = LanguageChoice.CultureFor(saved);

        // assert
        Assert.Equal(expected, culture?.Name);
    }

    [Fact(DisplayName = "Choosing a language in Settings saves it and loads the page again in it")]
    public void Picker_Choose_SavesAndReloads()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        _store.Saved = LanguageChoice.Portuguese;
        var picker = Render<LanguagePicker>();
        var shown = picker.Find("select").GetAttribute("value");

        // act
        picker.Find("select").Change(LanguageChoice.English);

        // assert
        Assert.Equal("pt", shown);
        Assert.Equal(LanguageChoice.English, _store.Saved);
        var navigation = Services.GetRequiredService<NavigationManager>();
        var reload = Assert.Single(((BunitNavigationManager)navigation).History);
        Assert.True(reload.Options.ForceLoad);
        Assert.Contains("Idioma", picker.Find("label").TextContent, StringComparison.Ordinal);
    }
}

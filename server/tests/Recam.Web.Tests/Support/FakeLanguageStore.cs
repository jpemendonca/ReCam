using Recam.Web.Localization;

namespace Recam.Web.Tests.Support;

public sealed class FakeLanguageStore : ILanguageStore
{
    public string Saved { get; set; } = LanguageChoice.Browser;

    public Task<string> ReadAsync() => Task.FromResult(Saved);

    public Task SaveAsync(string choice)
    {
        Saved = choice;
        return Task.CompletedTask;
    }
}

namespace Recam.Web.Localization;

/// <summary>Keeps the language choice in this browser. Faked in tests.</summary>
public interface ILanguageStore
{
    /// <summary>The saved choice, or <see cref="LanguageChoice.Browser"/> when there is none.</summary>
    Task<string> ReadAsync();

    Task SaveAsync(string choice);
}

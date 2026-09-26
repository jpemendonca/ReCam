using System.Globalization;

namespace Recam.Web.Tests.Support;

/// <summary>Switches the culture for one test, the way the browser's language does (texts, dates and numbers).</summary>
public sealed class Culture : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUi = CultureInfo.CurrentUICulture;

    private Culture(string name)
    {
        var culture = new CultureInfo(name);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static Culture Use(string name) => new(name);

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        CultureInfo.CurrentUICulture = _previousUi;
    }
}

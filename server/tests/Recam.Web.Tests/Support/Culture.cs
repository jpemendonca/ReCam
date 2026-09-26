using System.Globalization;

namespace Recam.Web.Tests.Support;

/// <summary>Switches the UI culture for one test, the way the browser's language does.</summary>
public sealed class Culture : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    private Culture(string name) => CultureInfo.CurrentUICulture = new CultureInfo(name);

    public static Culture Use(string name) => new(name);

    public void Dispose() => CultureInfo.CurrentUICulture = _previous;
}

using System.Reflection;

namespace Recam.Web.Localization;

/// <summary>This Monitor's build version, from Git at build time (scripts/version.sh), or "dev".</summary>
public static class AppVersion
{
    public static string Current { get; } =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
}

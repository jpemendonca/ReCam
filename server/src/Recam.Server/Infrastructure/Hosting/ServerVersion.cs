using System.Reflection;

namespace Recam.Server.Infrastructure.Hosting;

/// <summary>The build's version, from Git at build time (scripts/version.sh), or "dev".</summary>
public static class ServerVersion
{
    public static string Current { get; } =
        typeof(ServerVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
}

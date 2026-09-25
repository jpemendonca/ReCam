using Recam.Server.Infrastructure.Presence;

namespace Recam.Server.Infrastructure.Hosting;

public static class HostingExtensions
{
    public static IServiceCollection AddRecamHosting(this IServiceCollection services, ServerSettings settings)
    {
        services.AddSingleton(settings);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<DevicePresence>();
        return services;
    }
}

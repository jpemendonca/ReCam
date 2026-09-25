using Recam.Server.Infrastructure.Realtime;

namespace Recam.Server.Features.Realtime;

public static class RealtimeEndpoints
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<WatchLeases>();
        services.AddSingleton<DeviceConnections>();
        services.AddSingleton<IDeviceRemovals, RealtimeDeviceRemovals>();
        return services;
    }

    public static IEndpointRouteBuilder MapRealtimeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<DeviceHub>(DeviceHub.Path);
        return endpoints;
    }
}

namespace Recam.Server.Features.Realtime;

public static class RealtimeEndpoints
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    public static IEndpointRouteBuilder MapRealtimeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<DeviceHub>(DeviceHub.Path);
        return endpoints;
    }
}

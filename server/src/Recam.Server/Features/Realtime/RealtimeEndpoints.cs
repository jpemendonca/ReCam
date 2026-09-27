using System.Text.Json;
using System.Text.Json.Serialization;
using Recam.Server.Infrastructure.Realtime;

namespace Recam.Server.Features.Realtime;

public static class RealtimeEndpoints
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        // Enums go as camelCase names, as in the REST API.
        services.AddSignalR().AddJsonProtocol(options =>
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.AddSingleton<WatchLeases>();
        services.AddSingleton<RecordingStateWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<RecordingStateWorker>());
        services.AddSingleton<DeviceConnections>();
        services.AddSingleton<IDeviceRemovals, RealtimeDeviceRemovals>();
        services.AddSingleton<IDeviceListChanges, RealtimeDeviceListChanges>();
        return services;
    }

    public static IEndpointRouteBuilder MapRealtimeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<DeviceHub>(DeviceHub.Path);
        return endpoints;
    }
}

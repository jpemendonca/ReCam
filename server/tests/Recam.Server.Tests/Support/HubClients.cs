using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Recam.Server.Tests.Support;

public static class HubClients
{
    /// <summary>Opens a real SignalR connection to the in-memory server as the given device.</summary>
    public static async Task<HubConnection> ConnectAsync(this RecamApiFactory factory, string credential)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/devices"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(credential);
            })
            .Build();
        await connection.StartAsync(TestContext.Current.CancellationToken);
        return connection;
    }
}

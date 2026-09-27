using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Recam.Server.Tests.Support;

public static class HubClients
{
    /// <summary>Opens a real SignalR connection to the in-memory server as the given device.</summary>
    public static async Task<HubConnection> ConnectAsync(this RecamApiFactory factory, string credential)
    {
        var connection = factory.BuildConnection(credential);
        await connection.StartAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    /// <summary>Builds the connection without opening it, so handlers can be added first.</summary>
    public static HubConnection BuildConnection(this RecamApiFactory factory, string credential) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/devices"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(credential);
            })
            .WithCamelCaseEnums()
            .Build();

    /// <summary>Reads enums as the server sends them: camelCase names.</summary>
    public static IHubConnectionBuilder WithCamelCaseEnums(this IHubConnectionBuilder builder) =>
        builder.AddJsonProtocol(options =>
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
}

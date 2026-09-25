using Recam.Server.Features.Devices;
using Recam.Server.Features.Health;
using Recam.Server.Features.Media;
using Recam.Server.Features.Pairing;
using Recam.Server.Features.Realtime;
using Recam.Server.Features.Recordings;
using Recam.Server.Features.Setup;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Tls;

if (args is [HealthcheckCommand.Argument])
{
    return await HealthcheckCommand.RunAsync(CancellationToken.None);
}

var builder = WebApplication.CreateBuilder(args);
var settings = ServerSettings.From(builder.Configuration, builder.Environment.ContentRootPath);

builder.AddRecamTls(settings);
builder.Services
    .AddRecamHosting(settings)
    .AddRecamHttp()
    .AddRecamPersistence(settings)
    .AddRecamAuth()
    .AddSetup()
    .AddPairing()
    .AddRealtime()
    .AddMediaProxy(settings)
    .AddRecordings();

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.UseRecamHttp();

app.MapHealthEndpoints();
app.MapSetupEndpoints();
app.MapPairingEndpoints();
app.MapDeviceEndpoints();
app.MapRealtimeEndpoints();
app.MapMediaProxyEndpoints();
app.MapRecordingEndpoints();

await app.RunAsync();
return 0;

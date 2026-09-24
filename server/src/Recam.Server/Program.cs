using Recam.Server.Features.Health;
using Recam.Server.Features.Setup;
using Recam.Server.Infrastructure.Hosting;
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
    .AddRecamPersistence(settings)
    .AddSetup();

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.MapHealthEndpoints();
app.MapSetupEndpoints();

await app.RunAsync();
return 0;

public partial class Program;

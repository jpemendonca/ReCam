using Recam.Server.Features.Health;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Tls;

if (args is [HealthcheckCommand.Argument])
{
    return await HealthcheckCommand.RunAsync(CancellationToken.None);
}

var builder = WebApplication.CreateBuilder(args);
var settings = ServerSettings.From(builder.Configuration, builder.Environment.ContentRootPath);

builder.AddRecamTls(settings);

var app = builder.Build();

app.MapHealthEndpoints();

await app.RunAsync();
return 0;

public partial class Program;

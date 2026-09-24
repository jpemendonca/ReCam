using Recam.Server.Features.Health;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapHealthEndpoints();

app.Run();

public partial class Program;

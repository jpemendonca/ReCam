using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Tests.Support;

/// <summary>Runs the whole API in memory with a throwaway data directory.</summary>
public sealed class RecamApiFactory : WebApplicationFactory<Program>
{
    private readonly TemporaryDirectory _dataDirectory = new();

    public string DataDirectory => _dataDirectory.Path;

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting(ServerSettings.DataDirectoryKey, _dataDirectory.Path);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _dataDirectory.Dispose();
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Features.Setup;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Setup;

public sealed class FirstOpenCodeCommandTests
{
    private static readonly Uri[] ServerUrls = [new("https://192.168.0.10:8443")];

    [Fact(DisplayName = "code shows the address and the same first-time code the log printed, framed in the log")]
    public async Task Describe_WithoutMonitor_ShowsAddressAndCode()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var logged = await factory.FirstOpenCodeAsync();
        var settings = factory.Services.GetRequiredService<ServerSettings>();
        await using var database = await factory.CreateDatabaseAsync();

        // act
        var text = await FirstOpenCodeCommand.DescribeAsync(database, settings, ServerUrls, TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("Open https://192.168.0.10:8443 in the browser", text, StringComparison.Ordinal);
        Assert.Contains($"Type the first-time code {logged}", text, StringComparison.Ordinal);
        Assert.Contains(factory.Logs.Messages, message =>
            message.Contains("==================== ReCam ====================", StringComparison.Ordinal)
            && message.Contains(logged, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Once a browser is the Monitor, the code is gone and code says so")]
    public async Task Describe_WithMonitor_SaysThereIsNoCode()
    {
        // arrange
        using var factory = new RecamApiFactory();
        await factory.OpenBrowserMonitorAsync();
        var settings = factory.Services.GetRequiredService<ServerSettings>();
        await using var database = await factory.CreateDatabaseAsync();

        // act
        var text = await FirstOpenCodeCommand.DescribeAsync(database, settings, ServerUrls, TestContext.Current.CancellationToken);

        // assert
        Assert.StartsWith("This server already has a Monitor", text, StringComparison.Ordinal);
        Assert.Null(await FirstOpenCodeFile.ReadAsync(settings.DataDirectory, TestContext.Current.CancellationToken));
    }
}

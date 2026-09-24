using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Infrastructure.Tls;

public static class TlsExtensions
{
    public static WebApplicationBuilder AddRecamTls(this WebApplicationBuilder builder, ServerSettings settings)
    {
        var certificate = new CertificateStore(settings.DataDirectory, TimeProvider.System).GetOrCreate();
        builder.Services.AddSingleton(certificate);
        builder.WebHost.ConfigureKestrel(kestrel =>
            kestrel.ListenAnyIP(ServerSettings.HttpsPort, listen => listen.UseHttps(certificate.Certificate)));
        return builder;
    }
}

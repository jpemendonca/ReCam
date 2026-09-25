using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Infrastructure.Tls;

public static class TlsExtensions
{
    public static WebApplicationBuilder AddRecamTls(this WebApplicationBuilder builder, ServerSettings settings)
    {
        if (!settings.TlsEnabled)
        {
            // A reverse proxy in front terminates TLS and forwards plain HTTP to the same port.
            builder.Services.AddSingleton(new CertificatePin(null));
            builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenAnyIP(ServerSettings.HttpsPort));
            return builder;
        }

        var certificate = new CertificateStore(settings.DataDirectory, TimeProvider.System).GetOrCreate();
        builder.Services.AddSingleton(certificate);
        builder.Services.AddSingleton(new CertificatePin(certificate.Fingerprint));
        builder.WebHost.ConfigureKestrel(kestrel =>
            kestrel.ListenAnyIP(ServerSettings.HttpsPort, listen => listen.UseHttps(certificate.Certificate)));
        return builder;
    }
}

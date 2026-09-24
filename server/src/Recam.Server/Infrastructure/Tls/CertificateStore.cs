using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Recam.Server.Infrastructure.Tls;

public sealed class CertificateStore(string dataDirectory, TimeProvider timeProvider)
{
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
    private static readonly TimeSpan Validity = TimeSpan.FromDays(3650);

    private string PfxPath => Path.Combine(dataDirectory, "tls", "server.pfx");

    public ServerCertificate GetOrCreate()
    {
        var pfx = File.Exists(PfxPath) ? File.ReadAllBytes(PfxPath) : CreateAndSave();
        return ServerCertificate.FromPfx(pfx);
    }

    public ServerCertificate? Load() =>
        File.Exists(PfxPath) ? ServerCertificate.FromPfx(File.ReadAllBytes(PfxPath)) : null;

    private byte[] CreateAndSave()
    {
        var now = timeProvider.GetUtcNow();
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Recam", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(ServerAuthenticationOid)], false));

        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.Add(Validity));
        var pfx = certificate.Export(X509ContentType.Pfx);

        // Write then move, so a crash mid-write never leaves a truncated certificate behind.
        Directory.CreateDirectory(Path.GetDirectoryName(PfxPath)!);
        var temporaryPath = PfxPath + ".tmp";
        File.WriteAllBytes(temporaryPath, pfx);
        File.Move(temporaryPath, PfxPath, overwrite: true);
        return pfx;
    }
}

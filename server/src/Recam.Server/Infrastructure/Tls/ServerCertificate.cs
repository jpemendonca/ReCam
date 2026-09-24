using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Recam.Server.Infrastructure.Tls;

public sealed class ServerCertificate : IDisposable
{
    private ServerCertificate(X509Certificate2 certificate)
    {
        Certificate = certificate;
        Fingerprint = FingerprintOf(certificate);
    }

    public X509Certificate2 Certificate { get; }

    /// <summary>SHA-256 of the DER certificate, lowercase hex. Clients pin this value.</summary>
    public string Fingerprint { get; }

    public static ServerCertificate FromPfx(byte[] pfx) =>
        new(X509CertificateLoader.LoadPkcs12(pfx, password: null));

    public bool Matches(X509Certificate2 other) => FingerprintOf(other) == Fingerprint;

    public void Dispose() => Certificate.Dispose();

    private static string FingerprintOf(X509Certificate2 certificate) =>
        Convert.ToHexStringLower(SHA256.HashData(certificate.RawData));
}

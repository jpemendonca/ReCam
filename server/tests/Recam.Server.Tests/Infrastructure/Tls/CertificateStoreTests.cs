using Recam.Server.Infrastructure.Tls;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Infrastructure.Tls;

public sealed class CertificateStoreTests : IDisposable
{
    private readonly TemporaryDirectory _dataDirectory = new();

    public void Dispose() => _dataDirectory.Dispose();

    [Fact(DisplayName = "First run creates a ten-year certificate with a SHA-256 fingerprint")]
    public void GetOrCreate_OnFirstRun_CreatesPfx()
    {
        // arrange
        var store = new CertificateStore(_dataDirectory.Path, TimeProvider.System);
        var now = TimeProvider.System.GetUtcNow();

        // act
        using var certificate = store.GetOrCreate();

        // assert
        Assert.True(File.Exists(Path.Combine(_dataDirectory.Path, "tls", "server.pfx")));
        Assert.True(certificate.Certificate.HasPrivateKey);
        Assert.Matches("^[0-9a-f]{64}$", certificate.Fingerprint);
        Assert.InRange(certificate.Certificate.NotAfter.ToUniversalTime(), now.AddDays(3649).UtcDateTime, now.AddDays(3651).UtcDateTime);
    }

    [Fact(DisplayName = "Second run reuses the saved certificate")]
    public void GetOrCreate_OnSecondRun_ReusesSameFingerprint()
    {
        // arrange
        using var first = new CertificateStore(_dataDirectory.Path, TimeProvider.System).GetOrCreate();

        // act
        using var second = new CertificateStore(_dataDirectory.Path, TimeProvider.System).GetOrCreate();

        // assert
        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact(DisplayName = "Loading without a saved certificate returns nothing")]
    public void Load_WithoutPfx_ReturnsNull()
    {
        // arrange
        var store = new CertificateStore(_dataDirectory.Path, TimeProvider.System);

        // act
        using var certificate = store.Load();

        // assert
        Assert.Null(certificate);
    }

    [Fact(DisplayName = "A certificate matches itself and not a different one")]
    public void Matches_WithOtherCertificate_ReturnsFalse()
    {
        // arrange
        using var otherDirectory = new TemporaryDirectory();
        using var certificate = new CertificateStore(_dataDirectory.Path, TimeProvider.System).GetOrCreate();
        using var other = new CertificateStore(otherDirectory.Path, TimeProvider.System).GetOrCreate();

        // act
        var matchesSelf = certificate.Matches(certificate.Certificate);
        var matchesOther = certificate.Matches(other.Certificate);

        // assert
        Assert.True(matchesSelf);
        Assert.False(matchesOther);
    }
}

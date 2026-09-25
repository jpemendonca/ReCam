using Microsoft.EntityFrameworkCore;
using QRCoder;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Network;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Tls;

namespace Recam.Server.Features.Setup;

/// <summary>
/// Keeps exactly one valid owner pairing token while nobody owns the server. The secret lives
/// only in memory; the database keeps its hash.
/// </summary>
public sealed partial class OwnerSetup(
    IDbContextFactory<RecamDbContext> databaseFactory,
    TimeProvider timeProvider,
    ServerCertificate certificate,
    ServerSettings settings,
    ILogger<OwnerSetup> logger) : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private OwnerSetupStatus.Pending? _current;

    public async Task<OwnerSetupStatus> EnsureTokenAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
            var hasOwner = await database.Devices
                .AnyAsync(device => device.Role == DeviceRole.Owner && device.RevokedAt == null, cancellationToken);
            if (hasOwner)
            {
                _current = null;
                return new OwnerSetupStatus.Configured();
            }

            var now = timeProvider.GetUtcNow();
            if (_current is null || _current.ExpiresAt <= now)
            {
                _current = await IssueAsync(database, now, cancellationToken);
            }

            return _current;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    private async Task<OwnerSetupStatus.Pending> IssueAsync(RecamDbContext database, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await database.PairingTokens
            .Where(token => token.GrantsRole == DeviceRole.Owner && token.UsedAt == null)
            .ExecuteDeleteAsync(cancellationToken);

        var issued = PairingToken.Issue(DeviceRole.Owner, now);
        database.PairingTokens.Add(issued.Token);
        await database.SaveChangesAsync(cancellationToken);

        var serverUrls = PublicUrlResolver.Resolve(settings, PublicUrlResolver.DetectLocalAddresses());
        var pending = new OwnerSetupStatus.Pending(
            PairingUri.Build(issued.Secret, DeviceRole.Owner, certificate.Fingerprint, serverUrls),
            issued.Token.ExpiresAt);

        using var qrData = QRCodeGenerator.GenerateQrCode(pending.PairingUri, QRCodeGenerator.ECCLevel.L);
        using var asciiQr = new AsciiQRCode(qrData);
        var readableUrls = string.Join(", ", serverUrls.Select(url => url.GetLeftPart(UriPartial.Authority)));
        LogOwnerToken(logger, pending.ExpiresAt, readableUrls, Environment.NewLine + asciiQr.GetGraphicSmall() + Environment.NewLine, pending.PairingUri);
        return pending;
    }

    // The only log line allowed to carry a token (AGENTS.md): the operator reads it to claim the server.
    [LoggerMessage(Level = LogLevel.Warning, Message =
        "No owner paired yet. In the ReCam app, open the Watch tab and scan this QR code, or open " +
        "https://<server-ip>:8443/setup from your network. Expires at {ExpiresAt}. Server URLs: {ServerUrls}.{QrCode}" +
        "Without a camera, choose Paste code in the app and paste: {PairingUri}")]
    private static partial void LogOwnerToken(ILogger logger, DateTimeOffset expiresAt, string serverUrls, string qrCode, string pairingUri);
}

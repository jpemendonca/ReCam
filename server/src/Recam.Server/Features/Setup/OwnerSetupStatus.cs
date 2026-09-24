namespace Recam.Server.Features.Setup;

public abstract record OwnerSetupStatus
{
    private OwnerSetupStatus()
    {
    }

    public sealed record Configured : OwnerSetupStatus;

    public sealed record Pending(string PairingUri, DateTimeOffset ExpiresAt) : OwnerSetupStatus;
}

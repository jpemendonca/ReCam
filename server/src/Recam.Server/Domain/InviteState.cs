namespace Recam.Server.Domain;

/// <summary>Where a browser invitation stands, for the Monitor that made it.</summary>
public enum InviteState
{
    Waiting,
    Used,
    Expired,
}

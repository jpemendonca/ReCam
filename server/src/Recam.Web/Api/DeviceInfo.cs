namespace Recam.Web.Api;

/// <summary>A device on the server (GET /api/devices). Role is "owner", "viewer" or "camera".</summary>
public sealed record DeviceInfo(Guid Id, string Name, string Role, bool Online, DateTimeOffset? LastSeenAt = null)
{
    public bool IsCamera => Role == "camera";
}

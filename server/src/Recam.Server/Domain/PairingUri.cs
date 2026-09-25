using System.Text;

namespace Recam.Server.Domain;

/// <summary>The QR code payload defined in SPECS.md 5.2.</summary>
public static class PairingUri
{
    public const int Version = 1;

    public static string Build(
        string tokenSecret, DeviceRole grantsRole, string? certificateFingerprint, IEnumerable<Uri> serverUrls)
    {
        var uri = new StringBuilder($"recam://pair?v={Version}&t={Uri.EscapeDataString(tokenSecret)}")
            .Append("&r=").Append(RoleName(grantsRole));
        if (certificateFingerprint is not null)
        {
            uri.Append("&f=").Append(certificateFingerprint);
        }

        foreach (var serverUrl in serverUrls)
        {
            uri.Append("&u=").Append(Uri.EscapeDataString(serverUrl.GetLeftPart(UriPartial.Authority)));
        }

        return uri.ToString();
    }

    // The app picks the tab from this hint before calling the server.
    private static string RoleName(DeviceRole role) => role switch
    {
        DeviceRole.Owner => "owner",
        DeviceRole.Viewer => "viewer",
        DeviceRole.Camera => "camera",
        _ => throw new InvalidOperationException($"Unknown role {role}."),
    };
}

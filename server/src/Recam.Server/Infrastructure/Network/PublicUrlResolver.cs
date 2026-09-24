using System.Net;
using System.Net.NetworkInformation;
using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Infrastructure.Network;

/// <summary>Decides which server URLs go into pairing QR codes (SPECS.md 5.1).</summary>
public static class PublicUrlResolver
{
    public static IReadOnlyList<Uri> Resolve(ServerSettings settings, IEnumerable<IPAddress> localAddresses)
    {
        if (settings.PublicUrls.Count > 0)
        {
            return settings.PublicUrls;
        }

        if (settings.Host is not null)
        {
            return [HttpsUrl(settings.Host)];
        }

        return [.. localAddresses
            .Where(address => address.IsPrivateIPv4())
            .Distinct()
            .Select(address => HttpsUrl(address.ToString()))];
    }

    public static IEnumerable<IPAddress> DetectLocalAddresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up
                && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
            .Select(unicast => unicast.Address);

    private static Uri HttpsUrl(string host) => new UriBuilder(Uri.UriSchemeHttps, host, ServerSettings.HttpsPort).Uri;
}

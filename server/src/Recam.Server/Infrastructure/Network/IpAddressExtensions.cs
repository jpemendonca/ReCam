using System.Net;
using System.Net.Sockets;

namespace Recam.Server.Infrastructure.Network;

public static class IpAddressExtensions
{
    public static bool IsPrivateIPv4(this IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }
}

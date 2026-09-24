using System.Diagnostics.CodeAnalysis;

namespace Recam.Server.Domain;

/// <summary>
/// The bearer credential a paired device sends: "&lt;device id, N format&gt;.&lt;secret&gt;".
/// The id lets the server find the device without scanning every hash.
/// </summary>
public static class DeviceCredential
{
    private const char Separator = '.';

    public static string Format(Guid deviceId, string secret) => $"{deviceId:N}{Separator}{secret}";

    public static bool TryParse(string? credential, out Guid deviceId, [NotNullWhen(true)] out string? secret)
    {
        deviceId = Guid.Empty;
        secret = null;
        var separatorIndex = credential?.IndexOf(Separator, StringComparison.Ordinal) ?? -1;
        if (credential is null || separatorIndex <= 0 || separatorIndex == credential.Length - 1)
        {
            return false;
        }

        if (!Guid.TryParseExact(credential.AsSpan(0, separatorIndex), "N", out deviceId))
        {
            return false;
        }

        secret = credential[(separatorIndex + 1)..];
        return true;
    }
}

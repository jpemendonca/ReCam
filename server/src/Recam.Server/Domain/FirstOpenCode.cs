using System.Security.Cryptography;
using System.Text;

namespace Recam.Server.Domain;

/// <summary>
/// The code the operator reads in the server log to make the first browser the Monitor
/// (SPECS.md 6). It lives only in memory and proves access to the server's log; it is not a
/// credential. After <see cref="MaxAttempts"/> wrong tries it is spent, and a new one is issued.
/// </summary>
public sealed class FirstOpenCode
{
    public const int Length = 8;
    public const int MaxAttempts = 5;

    // No letters or digits that look alike (0/O, 1/I/L), since people type what they read.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly byte[] _value;
    private int _failures;

    private FirstOpenCode(string value) => _value = Encoding.ASCII.GetBytes(value);

    /// <summary>The code as the log shows it, in two halves: <c>ABCD-EFGH</c>.</summary>
    public string Display => $"{Encoding.ASCII.GetString(_value, 0, Length / 2)}-{Encoding.ASCII.GetString(_value, Length / 2, Length / 2)}";

    public bool IsSpent => _failures >= MaxAttempts;

    public static FirstOpenCode Issue() => new(RandomNumberGenerator.GetString(Alphabet, Length));

    /// <summary>
    /// Makes the browser that typed the right code the server's owner. Refused while any Monitor
    /// is active; that refusal does not count as a wrong try.
    /// </summary>
    public Result<PairedDevice> Open(string? typed, string browserName, bool serverHasMonitor, DateTimeOffset now)
    {
        if (serverHasMonitor)
        {
            return SetupErrors.AlreadyHasMonitor;
        }

        if (IsSpent || !Matches(typed))
        {
            _failures++;
            return SetupErrors.WrongCode;
        }

        return Device.CreateFirstMonitor(browserName, now);
    }

    // Case, spaces and the dash do not matter. Compared in constant time.
    private bool Matches(string? typed)
    {
        var normalized = new string((typed ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(char.ToUpperInvariant)
            .ToArray());
        return normalized.Length == Length
            && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(normalized), _value);
    }
}

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Recam.Server.Domain;

/// <summary>
/// High-entropy random secrets. SHA-256 is enough to store them: unlike passwords, 256 random
/// bits cannot be brute-forced, so a slow hash adds nothing.
/// </summary>
public static class SecretToken
{
    private const int SizeInBytes = 32;

    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SizeInBytes));

    public static byte[] Hash(string secret) => SHA256.HashData(Encoding.UTF8.GetBytes(secret));
}

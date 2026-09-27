using System.Security.Cryptography;
using Recam.Server.Infrastructure.Recordings;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Infrastructure.Recordings;

public sealed class RecordingCipherTests : IDisposable
{
    private readonly TemporaryDirectory _folder = new();
    private readonly RecordingCipher _cipher = new(RandomNumberGenerator.GetBytes(32));

    public void Dispose() => _folder.Dispose();

    private string PlainFile(byte[] content)
    {
        var path = Path.Combine(_folder.Path, "2026-09-27_10-00-00-000000.mp4");
        File.WriteAllBytes(path, content);
        return path;
    }

    [Fact(DisplayName = "A ciphered file reads back as the same bytes, and on disk has no MP4 signature left")]
    public void CipherFile_ThenOpenRead_GivesTheSameBytes()
    {
        // arrange
        var content = RandomNumberGenerator.GetBytes(200_000);
        "\0\0\0\u0018ftypisom"u8.CopyTo(content);
        var path = PlainFile(content);

        // act
        _cipher.CipherFile(path);
        using var read = _cipher.OpenRead(path);
        using var copy = new MemoryStream();
        read.CopyTo(copy);

        // assert
        Assert.Equal(content, copy.ToArray());
        var onDisk = File.ReadAllBytes(path);
        Assert.Equal(content.Length + RecordingCipher.HeaderLength, onDisk.Length);
        Assert.DoesNotContain("ftyp", System.Text.Encoding.ASCII.GetString(onDisk, 0, 64), StringComparison.Ordinal);
        Assert.True(RecordingCipher.IsCiphered(path));
    }

    [Theory(DisplayName = "Reading from the middle of a ciphered file gives the right bytes, as a player seeking does")]
    [InlineData(0, 10)]
    [InlineData(13, 40)]
    [InlineData(100_003, 5_000)]
    [InlineData(199_990, 10)]
    public void OpenRead_FromTheMiddle_GivesThatStretch(int offset, int count)
    {
        // arrange
        var content = RandomNumberGenerator.GetBytes(200_000);
        var path = PlainFile(content);
        _cipher.CipherFile(path);
        using var read = _cipher.OpenRead(path);
        var buffer = new byte[count];

        // act
        read.Seek(offset, SeekOrigin.Begin);
        read.ReadExactly(buffer);

        // assert
        Assert.Equal(content.AsSpan(offset, count).ToArray(), buffer);
        Assert.Equal(content.Length, read.Length);
    }

    [Fact(DisplayName = "Ciphering keeps the file's time and does nothing to a file already ciphered")]
    public void CipherFile_Twice_KeepsTimeAndCiphersOnce()
    {
        // arrange
        var path = PlainFile(RandomNumberGenerator.GetBytes(1_000));
        var modified = new DateTime(2026, 9, 27, 10, 1, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, modified);
        _cipher.CipherFile(path);
        var once = File.ReadAllBytes(path);

        // act
        _cipher.CipherFile(path);

        // assert
        Assert.Equal(once, File.ReadAllBytes(path));
        Assert.Equal(modified, File.GetLastWriteTimeUtc(path));
    }

    [Fact(DisplayName = "A plain file, like the one still being recorded, reads as it is")]
    public void OpenRead_PlainFile_ReadsAsItIs()
    {
        // arrange
        var content = RandomNumberGenerator.GetBytes(3_000);
        var path = PlainFile(content);

        // act
        using var read = _cipher.OpenRead(path);
        using var copy = new MemoryStream();
        read.CopyTo(copy);

        // assert
        Assert.Equal(content, copy.ToArray());
        Assert.False(RecordingCipher.IsCiphered(path));
    }
}

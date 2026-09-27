using System.Buffers.Binary;
using System.Security.Cryptography;
using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Infrastructure.Recordings;

/// <summary>
/// Keeps closed recording files unreadable to whoever opens the recordings folder (SPECS.md 2.4):
/// AES-CTR with a key kept in the data folder. It discourages the curious, not someone with root,
/// who can read the key too. CTR keeps the size and lets playback start anywhere in a file.
/// </summary>
/// <remarks>
/// A ciphered file is <c>RECAMENC</c>, a 16-byte random initial counter, then the ciphertext.
/// Anything else is a plain file, like the one MediaMTX is still writing.
/// </remarks>
public sealed class RecordingCipher
{
    public const string KeyFileName = "recordings.key";
    public const int HeaderLength = 8 + CounterLength;

    private const int CounterLength = 16;
    private const int BlockLength = 16;
    private const int KeyLength = 32;

    private readonly byte[] _key;

    public RecordingCipher(ServerSettings settings)
        : this(LoadOrCreateKey(Path.Combine(settings.DataDirectory, KeyFileName)))
    {
    }

    internal RecordingCipher(byte[] key) => _key = key;

    private static ReadOnlySpan<byte> Magic => "RECAMENC"u8;

    public static bool IsCiphered(string path)
    {
        using var file = File.OpenRead(path);
        Span<byte> start = stackalloc byte[8];
        return file.ReadAtLeast(start, start.Length, throwOnEndOfStream: false) == start.Length && start.SequenceEqual(Magic);
    }

    /// <summary>
    /// Replaces a plain file with its ciphered copy, keeping its modified time (the recording
    /// state reads it). A file already ciphered is left alone.
    /// </summary>
    public void CipherFile(string path)
    {
        if (IsCiphered(path))
        {
            return;
        }

        var modified = File.GetLastWriteTimeUtc(path);
        var counter = RandomNumberGenerator.GetBytes(CounterLength);
        var temporary = path + ".ciphering";
        using (var input = File.OpenRead(path))
        using (var output = File.Create(temporary))
        {
            output.Write(Magic);
            output.Write(counter);
            var buffer = new byte[64 * 1024];
            long offset = 0;
            int read;
            while ((read = input.Read(buffer)) > 0)
            {
                Transform(counter, offset, buffer.AsSpan(0, read));
                output.Write(buffer, 0, read);
                offset += read;
            }
        }

        File.SetLastWriteTimeUtc(temporary, modified);
        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>The file's plain bytes, seekable, whether it is ciphered or not.</summary>
    public Stream OpenRead(string path)
    {
        var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        Span<byte> header = stackalloc byte[HeaderLength];
        if (file.ReadAtLeast(header, HeaderLength, throwOnEndOfStream: false) < HeaderLength || !header[..8].SequenceEqual(Magic))
        {
            file.Position = 0;
            return file;
        }

        return new DecipheringStream(file, this, header[8..].ToArray());
    }

    /// <summary>XORs <paramref name="data"/>, which sits at <paramref name="offset"/> in the plain file, with the key stream.</summary>
    internal void Transform(byte[] counter, long offset, Span<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        var firstBlock = offset / BlockLength;
        var skip = (int)(offset % BlockLength);
        var blocks = (skip + data.Length + BlockLength - 1) / BlockLength;
        var counters = new byte[blocks * BlockLength];
        for (var index = 0; index < blocks; index++)
        {
            CounterAt(counter, firstBlock + index, counters.AsSpan(index * BlockLength, BlockLength));
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        var stream = aes.EncryptEcb(counters, PaddingMode.None);
        for (var index = 0; index < data.Length; index++)
        {
            data[index] ^= stream[skip + index];
        }
    }

    // The initial counter plus the block number, as one 128-bit big-endian number.
    private static void CounterAt(byte[] initial, long block, Span<byte> destination)
    {
        var high = BinaryPrimitives.ReadUInt64BigEndian(initial);
        var low = BinaryPrimitives.ReadUInt64BigEndian(initial.AsSpan(8));
        var sum = low + (ulong)block;
        if (sum < low)
        {
            high++;
        }

        BinaryPrimitives.WriteUInt64BigEndian(destination, high);
        BinaryPrimitives.WriteUInt64BigEndian(destination[8..], sum);
    }

    private static byte[] LoadOrCreateKey(string path)
    {
        if (File.Exists(path))
        {
            var saved = File.ReadAllBytes(path);
            if (saved.Length == KeyLength)
            {
                return saved;
            }

            throw new InvalidOperationException($"{path} is not a recordings key. Restore it from a backup, or delete it to lose the old recordings.");
        }

        var key = RandomNumberGenerator.GetBytes(KeyLength);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, key);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return key;
    }

    /// <summary>A ciphered file read as its plain bytes; seeking works, so Range requests do.</summary>
    private sealed class DecipheringStream(FileStream file, RecordingCipher cipher, byte[] counter) : Stream
    {
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => file.Length - HeaderLength;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            file.Position = HeaderLength + _position;
            var read = file.Read(buffer);
            cipher.Transform(counter, _position, buffer[..read]);
            _position += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            file.Position = HeaderLength + _position;
            var read = await file.ReadAsync(buffer, cancellationToken);
            cipher.Transform(counter, _position, buffer.Span[..read]);
            _position += read;
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin) => _position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            _ => Length + offset,
        };

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                file.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

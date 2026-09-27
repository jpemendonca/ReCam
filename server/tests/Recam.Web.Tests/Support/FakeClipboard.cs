using Recam.Web.Pairing;

namespace Recam.Web.Tests.Support;

public sealed class FakeClipboard : IClipboard
{
    public bool Refuses { get; set; }

    public List<string> Written { get; } = [];

    public Task<bool> WriteAsync(string text)
    {
        if (Refuses)
        {
            return Task.FromResult(false);
        }

        Written.Add(text);
        return Task.FromResult(true);
    }
}

using Recam.Web.Recordings;

namespace Recam.Web.Tests.Support;

public sealed class FakePeopleBoxesStore : IPeopleBoxesStore
{
    public bool Show { get; set; } = true;

    public Task<bool> ReadAsync() => Task.FromResult(Show);

    public Task SaveAsync(bool show)
    {
        Show = show;
        return Task.CompletedTask;
    }
}

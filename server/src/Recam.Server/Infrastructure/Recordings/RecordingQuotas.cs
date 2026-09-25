using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Persistence;

namespace Recam.Server.Infrastructure.Recordings;

public static class RecordingQuotas
{
    /// <summary>
    /// The stored quota, or a new default one. The default is tracked, so a caller that saves
    /// stores it; one that only reads just discards it.
    /// </summary>
    public static async Task<RecordingQuota> LoadAsync(RecamDbContext database, CancellationToken cancellationToken)
    {
        var quota = await database.RecordingQuotas.SingleOrDefaultAsync(cancellationToken);
        if (quota is not null)
        {
            return quota;
        }

        quota = RecordingQuota.CreateDefault();
        database.RecordingQuotas.Add(quota);
        return quota;
    }
}

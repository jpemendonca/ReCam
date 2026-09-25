using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Recordings;

namespace Recam.Server.Features.Recordings;

public static class RecordingEndpoints
{
    public static IServiceCollection AddRecordings(this IServiceCollection services)
    {
        services.AddSingleton<RecordingStore>();
        services.AddSingleton<RecordingCleanupWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<RecordingCleanupWorker>());
        return services;
    }

    public static IEndpointRouteBuilder MapRecordingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var quota = endpoints.MapGroup("/api/recordings/quota").RequireAuthorization(AuthExtensions.ViewerOrOwner);
        quota.MapGet(string.Empty, GetQuotaAsync);
        quota.MapPut(string.Empty, SetQuotaAsync);
        return endpoints;
    }

    private static async Task<Ok<QuotaResponse>> GetQuotaAsync(
        IDbContextFactory<RecamDbContext> databaseFactory, RecordingStore store, CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var quota = await RecordingQuotas.LoadAsync(database, cancellationToken);
        return TypedResults.Ok(new QuotaResponse(quota.Megabytes, UsedBytes(store), store.FreeBytes()));
    }

    private static async Task<IResult> SetQuotaAsync(
        QuotaRequest request,
        IDbContextFactory<RecamDbContext> databaseFactory,
        RecordingStore store,
        CancellationToken cancellationToken)
    {
        var validation = QuotaRequestValidator.Validate(request);
        if (validation.IsFailure)
        {
            return validation.Error.ToHttpResult();
        }

        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var quota = await RecordingQuotas.LoadAsync(database, cancellationToken);
        var changed = quota.ChangeTo(validation.Value, UsedBytes(store), store.FreeBytes());
        if (changed.IsFailure)
        {
            return changed.Error.ToHttpResult();
        }

        await database.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static long UsedBytes(RecordingStore store) => store.ListSegments().Sum(segment => segment.Bytes);
}

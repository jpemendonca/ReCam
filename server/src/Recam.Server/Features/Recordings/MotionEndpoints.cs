using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Presence;
using Recam.Server.Infrastructure.Recordings;

namespace Recam.Server.Features.Recordings;

/// <summary>
/// Motion in the recordings (SPECS.md 2.4): the motion service scores each closed segment, and
/// these routes turn the scores into events with the camera's sensitivity.
/// </summary>
public static class MotionEndpoints
{
    public static IEndpointRouteBuilder MapMotionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/cameras/{cameraId:guid}/motion", GetMotionAsync)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapPut("/api/cameras/{cameraId:guid}/motion-sensitivity", SetSensitivityAsync)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        return endpoints;
    }

    /// <summary>The events of one UTC day (<c>?day=AAAA-MM-DD</c>), from the segments that start in it.</summary>
    private static async Task<IResult> GetMotionAsync(
        Guid cameraId,
        string? day,
        IDbContextFactory<RecamDbContext> databaseFactory,
        RecordingStore store,
        DevicePresence presence,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return RecordingErrors.InvalidDay.ToHttpResult();
        }

        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var sensitivity = await database.Devices.AsNoTracking()
            .Where(device => device.Id == cameraId && device.Role == DeviceRole.Camera && device.RevokedAt == null)
            .Select(device => (MotionSensitivity?)device.MotionSensitivity)
            .SingleOrDefaultAsync(cancellationToken);
        if (sensitivity is null)
        {
            return MediaErrors.CameraNotFound.ToHttpResult();
        }

        var samples = store.ListSegments()
            .Where(segment => segment.CameraId == cameraId && DateOnly.FromDateTime(segment.StartsAt.UtcDateTime) == date)
            .SelectMany(store.ReadMotion);
        var events = MotionEvents.Find(samples, sensitivity.Value, [.. presence.TorchChanges(cameraId)]);
        return TypedResults.Ok(new MotionResponse(
            sensitivity.Value, [.. events.Select(found => new MotionEventResponse(found.Start, found.End, found.Peak))]));
    }

    private static async Task<IResult> SetSensitivityAsync(
        Guid cameraId,
        MotionSensitivityRequest request,
        ClaimsPrincipal user,
        IDbContextFactory<RecamDbContext> databaseFactory,
        CancellationToken cancellationToken)
    {
        if (request.Sensitivity is not { } sensitivity)
        {
            return DomainError.Validation(new Dictionary<string, string[]>
            {
                ["sensitivity"] = ["Choose low, medium or high."],
            }).ToHttpResult();
        }

        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var requesterId = user.GetDeviceId();
        var requester = await database.Devices.SingleAsync(device => device.Id == requesterId, cancellationToken);
        var camera = await database.Devices.SingleOrDefaultAsync(device => device.Id == cameraId, cancellationToken);
        if (camera is null)
        {
            return MediaErrors.CameraNotFound.ToHttpResult();
        }

        var changed = camera.SetMotionSensitivity(requester, sensitivity);
        if (changed.IsFailure)
        {
            return changed.Error.ToHttpResult();
        }

        await database.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}

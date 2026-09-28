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
/// these routes turn the scores into events with the camera's sensitivity. When the optional detect
/// service runs, each event also says whether a person was in it (SPECS.md 2.6).
/// </summary>
public static class MotionEndpoints
{
    public static IEndpointRouteBuilder MapMotionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/cameras/{cameraId:guid}/motion", GetMotionAsync)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapPut("/api/cameras/{cameraId:guid}/motion-sensitivity", SetSensitivityAsync)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapGet("/api/recordings/{cameraId:guid}/{segment}/people", GetSegmentPeople)
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

        var segments = store.ListSegments()
            .Where(segment => segment.CameraId == cameraId && DateOnly.FromDateTime(segment.StartsAt.UtcDateTime) == date)
            .ToList();
        var events = MotionEvents.Find(segments.SelectMany(store.ReadMotion), sensitivity.Value, [.. presence.TorchChanges(cameraId)]);
        List<SegmentPeople> people = [.. segments.Select(segment => new SegmentPeople(segment.StartsAt, store.ReadPeople(segment)))];
        return TypedResults.Ok(new MotionResponse(
            sensitivity.Value,
            [.. events.Select(found => new MotionEventResponse(found.Start, found.End, found.Peak, PeopleInMotion.HasPerson(found, people)))]));
    }

    /// <summary>
    /// The people found in one recording file, for the boxes drawn over the player: every second
    /// looked at, with only the boxes that count as a person. Not found until the detect service
    /// looked at the file, or when it does not run.
    /// </summary>
    private static IResult GetSegmentPeople(Guid cameraId, string segment, RecordingStore store)
    {
        if (store.PathOf(cameraId, segment) is not { } path || !File.Exists(path))
        {
            return RecordingErrors.SegmentNotFound.ToHttpResult();
        }

        var startsAt = RecordingStore.TryParseStart(segment)!.Value;
        if (store.ReadPeople(new RecordingSegment(cameraId, segment, startsAt, 0)) is not { } samples)
        {
            return RecordingErrors.PeopleNotAnalyzed.ToHttpResult();
        }

        return TypedResults.Ok(new SegmentPeopleResponse([.. samples.Select(sample => new PeopleSecondResponse(
            (sample.At - startsAt).TotalSeconds,
            [.. sample.People.Where(PeopleInMotion.IsPerson).Select(box => new PersonBoxResponse(box.X, box.Y, box.Width, box.Height))]))]));
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

namespace Recam.Server.Domain;

public static class MediaErrors
{
    public static readonly DomainError NotYourCamera =
        new("media.not_your_camera", "A camera can only publish its own stream.", ErrorType.Forbidden);

    public static readonly DomainError SignalingTooLarge =
        new("media.signaling_too_large", "The signaling message is too large.", ErrorType.Validation);

    public static readonly DomainError SessionNotFound =
        new("media.session_not_found", "There is no such media session.", ErrorType.NotFound);

    public static readonly DomainError CameraNotFound =
        new("media.camera_not_found", "There is no such camera.", ErrorType.NotFound);

    public static readonly DomainError RecordingNeedsH264 =
        new("media.recording_needs_h264", "This camera sends VP8, and only H.264 can be recorded.", ErrorType.Conflict);

    public static readonly DomainError CameraNotPublishing =
        new("media.camera_not_publishing", "The camera is not sending video right now.", ErrorType.Conflict);
}

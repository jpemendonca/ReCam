namespace Recam.Server.Domain;

public static class MediaErrors
{
    public static readonly DomainError NotYourCamera =
        new("media.not_your_camera", "A camera can only publish its own stream.", ErrorType.Forbidden);

    public static readonly DomainError SignalingTooLarge =
        new("media.signaling_too_large", "The signaling message is too large.", ErrorType.Validation);

    public static readonly DomainError CameraNotFound =
        new("media.camera_not_found", "There is no such camera.", ErrorType.NotFound);
}

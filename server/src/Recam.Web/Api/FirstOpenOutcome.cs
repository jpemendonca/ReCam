namespace Recam.Web.Api;

public enum FirstOpenOutcome
{
    Opened,
    WrongCode,
    AlreadyHasMonitor,
    NotLocal,
    TooManyAttempts,
}

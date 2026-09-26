namespace Recam.Server.Domain;

public static class SetupErrors
{
    public static readonly DomainError WrongCode =
        new("setup.wrong_code", "The first-time code is wrong. Check it in the server log.", ErrorType.Unauthorized);

    public static readonly DomainError AlreadyHasMonitor =
        new("setup.already_has_monitor", "This server already has a Monitor. Connect this browser from a Monitor phone.", ErrorType.Conflict);
}

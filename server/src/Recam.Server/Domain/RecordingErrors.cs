namespace Recam.Server.Domain;

public static class RecordingErrors
{
    public static readonly DomainError QuotaTooLarge =
        new("recording.quota_too_large", "There is not that much free disk space.", ErrorType.Validation)
        {
            Fields = new Dictionary<string, string[]> { ["quotaMb"] = ["There is not that much free disk space."] },
        };
}

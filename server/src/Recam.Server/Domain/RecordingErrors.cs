namespace Recam.Server.Domain;

public static class RecordingErrors
{
    public static readonly DomainError InvalidDay =
        new("recording.invalid_day", "The day must be written as AAAA-MM-DD.", ErrorType.Validation)
        {
            Fields = new Dictionary<string, string[]> { ["day"] = ["The day must be written as AAAA-MM-DD."] },
        };

    public static readonly DomainError SegmentNotFound =
        new("recording.segment_not_found", "There is no such recording.", ErrorType.NotFound);

    public static readonly DomainError QuotaTooLarge =
        new("recording.quota_too_large", "There is not that much free disk space.", ErrorType.Validation)
        {
            Fields = new Dictionary<string, string[]> { ["quotaMb"] = ["There is not that much free disk space."] },
        };
}

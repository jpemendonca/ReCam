using Recam.Server.Domain;

namespace Recam.Server.Features.Recordings;

public static class QuotaRequestValidator
{
    public static Result<int> Validate(QuotaRequest request)
    {
        if (request.QuotaMb is not { } megabytes || megabytes < RecordingQuota.MinimumMegabytes)
        {
            return DomainError.Validation(new Dictionary<string, string[]>
            {
                ["quotaMb"] = [$"The quota must be at least {RecordingQuota.MinimumMegabytes} MB."],
            });
        }

        return megabytes;
    }
}

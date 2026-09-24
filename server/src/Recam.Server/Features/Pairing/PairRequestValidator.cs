using Recam.Server.Domain;

namespace Recam.Server.Features.Pairing;

public static class PairRequestValidator
{
    /// <summary>Checks every field and reports all problems at once.</summary>
    public static Result<ValidPairRequest> Validate(PairRequest request)
    {
        var fields = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            fields["token"] = ["Token is required."];
        }

        var nameProblems = DeviceName.Validate(request.Name);
        if (nameProblems.Count > 0)
        {
            fields["name"] = [.. nameProblems];
        }

        if (fields.Count > 0)
        {
            return DomainError.Validation(fields);
        }

        return new ValidPairRequest(request.Token!.Trim(), request.Name!.Trim());
    }
}

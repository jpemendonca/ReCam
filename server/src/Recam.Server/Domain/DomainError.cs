namespace Recam.Server.Domain;

/// <summary>An expected failure: something the caller did or asked for, never a bug.</summary>
public sealed record DomainError(string Code, string Message, ErrorType Type)
{
    public IReadOnlyDictionary<string, string[]> Fields { get; init; } = new Dictionary<string, string[]>();

    public static DomainError Validation(IReadOnlyDictionary<string, string[]> fields) =>
        new("validation", "One or more fields are invalid.", ErrorType.Validation) { Fields = fields };
}

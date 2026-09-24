namespace Recam.Server.Domain;

/// <summary>Outcome of an operation that can fail for an expected reason.</summary>
public class Result
{
    private readonly DomainError? _error;

    protected Result(DomainError? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    public DomainError Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(null);

    public static Result Failure(DomainError error) => new(error);

    public static implicit operator Result(DomainError error) => Failure(error);
}

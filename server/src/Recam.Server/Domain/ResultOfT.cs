namespace Recam.Server.Domain;

/// <summary>Outcome that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(null)
    {
        _value = value;
    }

    private Result(DomainError error)
        : base(error)
    {
    }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(DomainError error) => new(error);
}

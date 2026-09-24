using Recam.Server.Domain;

namespace Recam.Server.Infrastructure.Http;

/// <summary>The single place where domain errors become HTTP responses (RFC 9457 problem details).</summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this DomainError error) => error.Type switch
    {
        ErrorType.Validation => TypedResults.ValidationProblem(
            error.Fields,
            title: error.Message,
            extensions: CodeExtension(error)),
        _ => TypedResults.Problem(
            title: error.Message,
            statusCode: StatusCodeOf(error.Type),
            extensions: CodeExtension(error)),
    };

    private static int StatusCodeOf(ErrorType type) => type switch
    {
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => throw new InvalidOperationException($"Unmapped error type {type}."),
    };

    private static Dictionary<string, object?> CodeExtension(DomainError error) => new() { ["code"] = error.Code };
}

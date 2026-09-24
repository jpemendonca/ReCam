namespace Recam.Server.Domain;

public static class DeviceName
{
    public const int MaxLength = 40;

    /// <summary>Returns every rule the name breaks; an empty list means the name is valid.</summary>
    public static IReadOnlyList<string> Validate(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        var problems = new List<string>();
        if (trimmed.Length == 0)
        {
            problems.Add("Name is required.");
        }

        if (trimmed.Length > MaxLength)
        {
            problems.Add($"Name must have at most {MaxLength} characters.");
        }

        if (trimmed.Any(char.IsControl))
        {
            problems.Add("Name must not contain control characters.");
        }

        return problems;
    }
}

namespace EcoBilling.SharedKernel.Errors;

public sealed record Error
{
    public static Error None { get; } = new();

    private Error()
    {
        Code = string.Empty;
        Description = string.Empty;
        Type = ErrorType.None;
    }

    public Error(string code, string description, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (!Enum.IsDefined(type) || type is ErrorType.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Only Error.None can have the None error type.");
        }

        Code = code.Trim();
        Description = description.Trim();
        Type = type;
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    public bool IsNone => Type is ErrorType.None;
}

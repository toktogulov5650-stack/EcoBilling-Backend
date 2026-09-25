using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Domain;

public sealed record LoginIdentity
{
    private LoginIdentity(LoginType type, string normalizedValue)
    {
        Type = type;
        NormalizedValue = normalizedValue;
    }

    private LoginIdentity()
    {
        NormalizedValue = string.Empty;
    }

    public LoginType Type { get; private set; }

    public string NormalizedValue { get; private set; }

    public static Result<LoginIdentity> Create(LoginType type, string? value)
    {
        if (!Enum.IsDefined(type))
        {
            return Result<LoginIdentity>.Failure(IdentityErrors.InvalidLogin);
        }

        var normalizedValue = Normalize(type, value);
        return normalizedValue.IsFailure
            ? Result<LoginIdentity>.Failure(normalizedValue.Error)
            : Result<LoginIdentity>.Success(new LoginIdentity(type, normalizedValue.Value));
    }

    public static Result<string> Normalize(LoginType type, string? value)
    {
        if (!Enum.IsDefined(type) || string.IsNullOrWhiteSpace(value))
        {
            return Result<string>.Failure(IdentityErrors.InvalidLogin);
        }

        var trimmedValue = value.Trim();

        if (type is LoginType.Email)
        {
            var atIndex = trimmedValue.IndexOf('@');
            var hasSingleAtSign = atIndex == trimmedValue.LastIndexOf('@');
            var hasLocalAndDomainParts = atIndex > 0 && atIndex < trimmedValue.Length - 1;
            var containsWhitespace = trimmedValue.Any(char.IsWhiteSpace);

            if (!hasSingleAtSign || !hasLocalAndDomainParts || containsWhitespace)
            {
                return Result<string>.Failure(IdentityErrors.InvalidLogin);
            }

            return Result<string>.Success(trimmedValue.ToUpperInvariant());
        }

        return Result<string>.Success(trimmedValue.ToUpperInvariant());
    }

    public bool IsCompatibleWith(UserRole role) => IsCompatible(role, Type);

    private static bool IsCompatible(UserRole role, LoginType type) => role switch
    {
        UserRole.Resident => type is LoginType.AccountNumber,
        UserRole.Controller or UserRole.Director => type is LoginType.Email,
        _ => false
    };
}

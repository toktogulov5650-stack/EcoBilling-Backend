using EcoBilling.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace EcoBilling.Infrastructure.Authentication;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly PasswordHashSubject Subject = new();
    private readonly PasswordHasher<PasswordHashSubject> passwordHasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return passwordHasher.HashPassword(Subject, password);
    }

    public PasswordVerificationOutcome Verify(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return passwordHasher.VerifyHashedPassword(Subject, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded =>
                PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed
        };
    }

    private sealed class PasswordHashSubject;
}

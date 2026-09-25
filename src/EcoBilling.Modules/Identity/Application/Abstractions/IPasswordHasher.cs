namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationOutcome Verify(string password, string passwordHash);
}

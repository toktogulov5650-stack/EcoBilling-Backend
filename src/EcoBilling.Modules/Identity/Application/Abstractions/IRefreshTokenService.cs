namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IRefreshTokenService
{
    GeneratedRefreshToken Generate();

    string Hash(string token);
}

public sealed record GeneratedRefreshToken(string Value, string Hash);

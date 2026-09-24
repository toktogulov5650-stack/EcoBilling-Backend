using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Authenticate;

public sealed class AuthenticateCommand
{
    public AuthenticateCommand(LoginType loginType, string? login, string? password)
    {
        LoginType = loginType;
        Login = login;
        Password = password;
    }

    public LoginType LoginType { get; }

    public string? Login { get; }

    public string? Password { get; }

    public override string ToString() => $"{nameof(AuthenticateCommand)} {{ credentials = [REDACTED] }}";
}

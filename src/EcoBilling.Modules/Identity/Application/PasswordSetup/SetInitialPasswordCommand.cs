using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.PasswordSetup;

public sealed record SetInitialPasswordCommand(
    LoginType LoginType,
    string? Login,
    string? InitialCredential,
    string? NewPassword)
{
    public override string ToString() =>
        $"{nameof(SetInitialPasswordCommand)} {{ LoginType = {LoginType}, Login = [REDACTED], InitialCredential = [REDACTED], NewPassword = [REDACTED] }}";
}

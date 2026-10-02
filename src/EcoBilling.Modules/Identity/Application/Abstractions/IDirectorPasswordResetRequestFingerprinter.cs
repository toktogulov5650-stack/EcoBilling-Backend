namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IDirectorPasswordResetRequestFingerprinter
{
    string Create(string normalizedEmail, string newPassword);
}

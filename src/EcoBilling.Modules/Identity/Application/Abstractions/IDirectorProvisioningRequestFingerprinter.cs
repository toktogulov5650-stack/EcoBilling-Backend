namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IDirectorProvisioningRequestFingerprinter
{
    string Create(
        string fullName,
        string normalizedEmail,
        string initialCredential);
}

namespace EcoBilling.Modules.Controllers.Features.Abstractions;

public interface IControllerCreationRequestFingerprinter
{
    string Create(
        string fullName,
        string normalizedEmail,
        string initialCredential);
}

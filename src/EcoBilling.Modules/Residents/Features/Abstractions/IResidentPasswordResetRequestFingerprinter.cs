using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public interface IResidentPasswordResetRequestFingerprinter
{
    string Create(ResidentId residentId, string newPassword);
}

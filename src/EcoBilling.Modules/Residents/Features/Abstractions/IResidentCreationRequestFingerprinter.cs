using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public interface IResidentCreationRequestFingerprinter
{
    string Create(
        string fullName,
        string normalizedAccountNumber,
        string password,
        Address address);
}

using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface ILoginNormalizer
{
    Result<LoginIdentity> Normalize(LoginType loginType, string? login);
}

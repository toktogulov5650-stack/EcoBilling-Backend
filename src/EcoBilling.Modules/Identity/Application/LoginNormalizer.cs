using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application;

public sealed class LoginNormalizer : ILoginNormalizer
{
    public Result<LoginIdentity> Normalize(LoginType loginType, string? login) =>
        LoginIdentity.Create(loginType, login);
}

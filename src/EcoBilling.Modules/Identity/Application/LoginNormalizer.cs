using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application;

public sealed class LoginNormalizer : ILoginNormalizer
{
    public Result<string> Normalize(LoginType loginType, string? login) =>
        LoginIdentity.Normalize(loginType, login);
}

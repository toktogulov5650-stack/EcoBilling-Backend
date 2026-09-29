using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Contracts;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Errors;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.GetDirectorProfile;

public sealed class GetDirectorProfileHandler(IDirectorProfileReader reader)
{
    private static readonly Error NotFound = new(
        "director.not_found",
        "The director profile was not found.",
        ErrorType.NotFound);

    public async Task<Result<DirectorProfileDetails>> Handle(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var profile = await reader.GetByUserIdAsync(userId, cancellationToken);
        return profile is null
            ? Result<DirectorProfileDetails>.Failure(NotFound)
            : Result<DirectorProfileDetails>.Success(profile);
    }
}

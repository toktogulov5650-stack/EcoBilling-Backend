using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Domain;

public sealed class Resident
{
    private Resident()
    {
        Id = null!;
        UserId = null!;
        FullName = string.Empty;
    }

    private Resident(
        ResidentId id,
        UserId userId,
        string fullName,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
        CreatedAt = createdAt;
    }

    public ResidentId Id { get; private set; }

    public UserId UserId { get; private set; }

    public string FullName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Resident> Create(
        ResidentId id,
        UserAccount userAccount,
        string? fullName,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(userAccount);

        if (userAccount.Role is not UserRole.Resident)
        {
            return Result<Resident>.Failure(ResidentErrors.IdentityMustHaveResidentRole);
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<Resident>.Failure(ResidentErrors.InvalidFullName);
        }

        return Result<Resident>.Success(
            new Resident(
                id,
                userAccount.Id,
                fullName.Trim(),
                createdAt.ToUniversalTime()));
    }
}

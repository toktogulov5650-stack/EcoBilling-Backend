using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Domain;

public sealed class DirectorProfile
{
    public const short InstanceSlot = 1;

    private DirectorProfile()
    {
        Id = null!;
        UserId = null!;
        FullName = string.Empty;
        Slot = InstanceSlot;
    }

    private DirectorProfile(
        DirectorId id,
        UserId userId,
        string fullName,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
        Slot = InstanceSlot;
        CreatedAt = createdAt;
    }

    public DirectorId Id { get; private set; }

    public UserId UserId { get; private set; }

    public string FullName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public short Slot { get; private set; }

    public static Result<DirectorProfile> Create(
        DirectorId id,
        UserAccount userAccount,
        string? fullName,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(userAccount);

        if (userAccount.Role is not UserRole.Director)
        {
            return Result<DirectorProfile>.Failure(
                DirectorProvisioningErrors.IdentityMustHaveDirectorRole);
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<DirectorProfile>.Failure(
                DirectorProvisioningErrors.InvalidFullName);
        }

        return Result<DirectorProfile>.Success(
            new DirectorProfile(
                id,
                userAccount.Id,
                fullName.Trim(),
                createdAt.ToUniversalTime()));
    }
}

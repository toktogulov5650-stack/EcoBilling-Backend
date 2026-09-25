using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Domain;

public sealed class Controller
{
    private Controller()
    {
        Id = null!;
        UserId = null!;
        FullName = string.Empty;
    }

    private Controller(
        ControllerId id,
        UserId userId,
        string fullName,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
        CreatedAt = createdAt;
    }

    public ControllerId Id { get; private set; }

    public UserId UserId { get; private set; }

    public string FullName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Controller> Create(
        ControllerId id,
        UserAccount userAccount,
        string? fullName,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(userAccount);

        if (userAccount.Role is not UserRole.Controller)
        {
            return Result<Controller>.Failure(ControllerErrors.IdentityMustHaveControllerRole);
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<Controller>.Failure(ControllerErrors.InvalidFullName);
        }

        return Result<Controller>.Success(
            new Controller(
                id,
                userAccount.Id,
                fullName.Trim(),
                createdAt.ToUniversalTime()));
    }
}

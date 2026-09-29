using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Domain;

public sealed class ControllerAssignment
{
    private ControllerAssignment()
    {
        Id = null!;
        ControllerId = null!;
        AddressId = null!;
    }

    private ControllerAssignment(
        ControllerAssignmentId id,
        ControllerId controllerId,
        AddressId addressId,
        DateTimeOffset createdAt)
    {
        Id = id;
        ControllerId = controllerId;
        AddressId = addressId;
        CreatedAt = createdAt;
    }

    public ControllerAssignmentId Id { get; private set; }

    public ControllerId ControllerId { get; private set; }

    public AddressId AddressId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<ControllerAssignment> Create(
        ControllerAssignmentId id,
        ControllerId controllerId,
        AddressId addressId,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(controllerId);
        ArgumentNullException.ThrowIfNull(addressId);

        return Result<ControllerAssignment>.Success(
            new ControllerAssignment(
                id,
                controllerId,
                addressId,
                createdAt.ToUniversalTime()));
    }
}

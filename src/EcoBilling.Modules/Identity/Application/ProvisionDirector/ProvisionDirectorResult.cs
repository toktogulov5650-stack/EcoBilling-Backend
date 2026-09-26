using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.ProvisionDirector;

public sealed record ProvisionDirectorResult(
    DirectorId DirectorId,
    DirectorProvisioningOperationId OperationId,
    bool IsReplay);

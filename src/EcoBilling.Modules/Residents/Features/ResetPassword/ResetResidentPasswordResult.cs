using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.ResetPassword;

public sealed record ResetResidentPasswordResult(
    ResidentPasswordResetOperationId OperationId,
    bool IsReplay);

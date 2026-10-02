using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.ResetDirectorPassword;

public sealed record ResetDirectorPasswordResult(
    DirectorPasswordResetOperationId OperationId,
    bool IsReplay);

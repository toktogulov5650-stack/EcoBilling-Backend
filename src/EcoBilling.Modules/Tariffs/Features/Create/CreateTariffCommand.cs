namespace EcoBilling.Modules.Tariffs.Features.Create;

public sealed record CreateTariffCommand(
    string? Name,
    string ActorId,
    string CorrelationId);

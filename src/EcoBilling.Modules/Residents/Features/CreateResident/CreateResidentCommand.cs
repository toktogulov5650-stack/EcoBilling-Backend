namespace EcoBilling.Modules.Residents.Features.CreateResident;

public sealed class CreateResidentCommand
{
    public CreateResidentCommand(
        string? idempotencyKey,
        string? fullName,
        string? accountNumber,
        string? password,
        string? locality,
        string? street,
        string? house,
        string? building,
        string? apartment,
        string actorId,
        string correlationId)
    {
        IdempotencyKey = idempotencyKey;
        FullName = fullName;
        AccountNumber = accountNumber;
        Password = password;
        Locality = locality;
        Street = street;
        House = house;
        Building = building;
        Apartment = apartment;
        ActorId = actorId;
        CorrelationId = correlationId;
    }

    public string? IdempotencyKey { get; }
    public string? FullName { get; }
    public string? AccountNumber { get; }
    public string? Password { get; }
    public string? Locality { get; }
    public string? Street { get; }
    public string? House { get; }
    public string? Building { get; }
    public string? Apartment { get; }
    public string ActorId { get; }
    public string CorrelationId { get; }

    public override string ToString() =>
        $"{nameof(CreateResidentCommand)} {{ IdempotencyKey = [REDACTED], FullName = [REDACTED], AccountNumber = [REDACTED], Password = [REDACTED], Address = [REDACTED], ActorId = [REDACTED], CorrelationId = [REDACTED] }}";
}

using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Domain;

public sealed class AccountTariffAssignment
{
    private AccountTariffAssignment()
    {
        Id = null!;
        AccountId = null!;
        TariffId = null!;
    }

    private AccountTariffAssignment(
        AccountTariffAssignmentId id,
        AccountId accountId,
        TariffId tariffId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        TariffId = tariffId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public AccountTariffAssignmentId Id { get; private set; }

    public AccountId AccountId { get; private set; }

    public TariffId TariffId { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }

    public DateOnly? EffectiveTo { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsEffectiveOn(DateOnly date) =>
        EffectiveFrom <= date &&
        (EffectiveTo is null || date < EffectiveTo.Value);

    public Result Close(DateOnly effectiveTo)
    {
        if (effectiveTo <= EffectiveFrom)
        {
            return Result.Failure(
                AccountTariffAssignmentErrors.InvalidEffectivePeriod);
        }

        if (EffectiveTo is not null && EffectiveTo.Value <= effectiveTo)
        {
            return Result.Success();
        }

        EffectiveTo = effectiveTo;
        return Result.Success();
    }

    public static Result<AccountTariffAssignment> Create(
        AccountTariffAssignmentId id,
        AccountId accountId,
        TariffId tariffId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(tariffId);

        if (effectiveTo is not null && effectiveTo.Value <= effectiveFrom)
        {
            return Result<AccountTariffAssignment>.Failure(
                AccountTariffAssignmentErrors.InvalidEffectivePeriod);
        }

        return Result<AccountTariffAssignment>.Success(
            new AccountTariffAssignment(
                id,
                accountId,
                tariffId,
                effectiveFrom,
                effectiveTo,
                createdAt));
    }
}

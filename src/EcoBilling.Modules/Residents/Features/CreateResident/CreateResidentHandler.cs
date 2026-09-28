using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.CreateResident;

public sealed class CreateResidentHandler
{
    private readonly IResidentCreationRepository repository;
    private readonly IResidentCreationRequestFingerprinter requestFingerprinter;
    private readonly IPasswordHasher passwordHasher;
    private readonly PasswordPolicy passwordPolicy;
    private readonly TimeProvider timeProvider;

    public CreateResidentHandler(
        IResidentCreationRepository repository,
        IResidentCreationRequestFingerprinter requestFingerprinter,
        IPasswordHasher passwordHasher,
        PasswordPolicy passwordPolicy,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.requestFingerprinter = requestFingerprinter
            ?? throw new ArgumentNullException(nameof(requestFingerprinter));
        this.passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        this.passwordPolicy = passwordPolicy
            ?? throw new ArgumentNullException(nameof(passwordPolicy));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<CreateResidentResult>> Handle(
        CreateResidentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) ||
            command.IdempotencyKey.Length >
            ResidentCreationOperation.MaximumIdempotencyKeyLength)
        {
            return Result<CreateResidentResult>.Failure(
                ResidentErrors.InvalidCreationIdempotencyKey);
        }

        if (!passwordPolicy.IsValid(command.Password))
        {
            return Result<CreateResidentResult>.Failure(
                ResidentErrors.InvalidPassword);
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            return Result<CreateResidentResult>.Failure(
                ResidentErrors.InvalidFullName);
        }

        var loginIdentity = LoginIdentity.Create(
            LoginType.AccountNumber,
            command.AccountNumber);
        if (loginIdentity.IsFailure)
        {
            return Result<CreateResidentResult>.Failure(loginIdentity.Error);
        }

        var address = Address.Create(
            new AddressId(Guid.NewGuid()),
            command.Locality,
            command.Street,
            command.House,
            command.Building,
            command.Apartment);
        if (address.IsFailure)
        {
            return Result<CreateResidentResult>.Failure(address.Error);
        }

        var createdAt = timeProvider.GetUtcNow();
        var userAccount = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity.Value,
            passwordHasher.Hash(command.Password!),
            UserRole.Resident,
            createdAt,
            requiresPasswordChange: false);
        if (userAccount.IsFailure)
        {
            return Result<CreateResidentResult>.Failure(userAccount.Error);
        }

        var resident = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            userAccount.Value,
            command.FullName,
            createdAt);
        if (resident.IsFailure)
        {
            return Result<CreateResidentResult>.Failure(resident.Error);
        }

        var account = Account.Create(
            new AccountId(Guid.NewGuid()),
            resident.Value.Id,
            address.Value.Id,
            loginIdentity.Value.NormalizedValue,
            createdAt);
        if (account.IsFailure)
        {
            return Result<CreateResidentResult>.Failure(account.Error);
        }

        var operation = ResidentCreationOperation.Create(
            new ResidentCreationOperationId(Guid.NewGuid()),
            command.IdempotencyKey,
            requestFingerprinter.Create(
                resident.Value.FullName,
                account.Value.Number.Value,
                command.Password!,
                address.Value),
            resident.Value.Id,
            account.Value.Id,
            address.Value.Id,
            createdAt);
        if (operation.IsFailure)
        {
            return Result<CreateResidentResult>.Failure(operation.Error);
        }

        var persistenceResult = await repository.CreateAsync(
            userAccount.Value,
            resident.Value,
            address.Value,
            account.Value,
            operation.Value,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return persistenceResult.Outcome switch
        {
            ResidentCreationPersistenceOutcome.Created => Success(
                operation.Value.ResidentId,
                operation.Value.AccountId,
                operation.Value.AddressId,
                operation.Value.Id,
                isReplay: false),
            ResidentCreationPersistenceOutcome.Replayed => Success(
                persistenceResult.ResidentId!,
                persistenceResult.AccountId!,
                persistenceResult.AddressId!,
                persistenceResult.OperationId!,
                isReplay: true),
            ResidentCreationPersistenceOutcome.IdempotencyConflict =>
                Result<CreateResidentResult>.Failure(
                    ResidentErrors.CreationIdempotencyConflict),
            ResidentCreationPersistenceOutcome.AccountNumberAlreadyExists =>
                Result<CreateResidentResult>.Failure(
                    ResidentErrors.AccountNumberAlreadyExists),
            _ => throw new InvalidOperationException(
                $"Unknown resident creation outcome: {persistenceResult.Outcome}.")
        };
    }

    private static Result<CreateResidentResult> Success(
        ResidentId residentId,
        AccountId accountId,
        AddressId addressId,
        ResidentCreationOperationId operationId,
        bool isReplay) =>
        Result<CreateResidentResult>.Success(
            new CreateResidentResult(
                residentId,
                accountId,
                addressId,
                operationId,
                isReplay));
}

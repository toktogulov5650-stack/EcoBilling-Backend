using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.Modules.Residents.Features.CreateResident;

namespace EcoBilling.UnitTests.Residents.Features.CreateResident;

public sealed class CreateResidentHandlerTests
{
    private const string Password = "resident-password-0123456789";
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WithValidRequest_CreatesAggregateAtomically()
    {
        var repository = new RecordingRepository(
            ResidentCreationPersistenceOutcome.Created);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReplay);
        Assert.Equal(Password, passwordHasher.ReceivedPassword);
        Assert.Equal(UserRole.Resident, repository.UserAccount.Role);
        Assert.Equal(
            "AB-000001",
            repository.UserAccount.LoginIdentity.NormalizedValue);
        Assert.False(repository.UserAccount.RequiresPasswordChange);
        Assert.Equal("stored-password-hash", repository.UserAccount.PasswordHash);
        Assert.Equal(UtcNow, repository.UserAccount.CreatedAt);
        Assert.Equal("Ada Lovelace", repository.Resident.FullName);
        Assert.Equal(repository.UserAccount.Id, repository.Resident.UserId);
        Assert.Equal(repository.Resident.Id, repository.Account.ResidentId);
        Assert.Equal(repository.Address.Id, repository.Account.AddressId);
        Assert.Equal("AB-000001", repository.Account.Number.Value);
        Assert.Equal("Bishkek", repository.Address.Locality);
        Assert.Equal("Chuy Avenue", repository.Address.Street);
        Assert.Equal("42", repository.Address.House);
        Assert.Equal("2", repository.Address.Building);
        Assert.Equal("17", repository.Address.Apartment);
        Assert.Equal(repository.Resident.Id, repository.Operation.ResidentId);
        Assert.Equal(repository.Account.Id, repository.Operation.AccountId);
        Assert.Equal(repository.Address.Id, repository.Operation.AddressId);
        Assert.Equal("director-user-id", repository.ActorId);
        Assert.Equal("trace-id", repository.CorrelationId);
    }

    [Fact]
    public async Task Handle_WhenRequestIsReplayed_ReturnsStoredIdentifiers()
    {
        var residentId = new ResidentId(Guid.NewGuid());
        var accountId = new AccountId(Guid.NewGuid());
        var addressId = new AddressId(Guid.NewGuid());
        var operationId = new ResidentCreationOperationId(Guid.NewGuid());
        var handler = CreateHandler(
            new RecordingRepository(
                ResidentCreationPersistenceOutcome.Replayed,
                residentId,
                accountId,
                addressId,
                operationId),
            new RecordingPasswordHasher());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReplay);
        Assert.Equal(residentId, result.Value.ResidentId);
        Assert.Equal(accountId, result.Value.AccountId);
        Assert.Equal(addressId, result.Value.AddressId);
        Assert.Equal(operationId, result.Value.OperationId);
    }

    [Theory]
    [InlineData(
        ResidentCreationPersistenceOutcome.IdempotencyConflict,
        "resident.creation.idempotency_conflict")]
    [InlineData(
        ResidentCreationPersistenceOutcome.AccountNumberAlreadyExists,
        "resident.account_number_already_exists")]
    public async Task Handle_MapsPersistenceConflict(
        ResidentCreationPersistenceOutcome outcome,
        string expectedCode)
    {
        var handler = CreateHandler(
            new RecordingRepository(outcome),
            new RecordingPasswordHasher());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Theory]
    [InlineData(null, "Ada Lovelace", "AB-000001", Password, "Bishkek", "Chuy", "42", "resident.creation.invalid_idempotency_key")]
    [InlineData("key", null, "AB-000001", Password, "Bishkek", "Chuy", "42", "resident.invalid_full_name")]
    [InlineData("key", "Ada Lovelace", null, Password, "Bishkek", "Chuy", "42", "auth.invalid_login")]
    [InlineData("key", "Ada Lovelace", "AB-000001", "short", "Bishkek", "Chuy", "42", "resident.invalid_password")]
    [InlineData("key", "Ada Lovelace", "AB-000001", Password, null, "Chuy", "42", "address.invalid_locality")]
    [InlineData("key", "Ada Lovelace", "AB-000001", Password, "Bishkek", null, "42", "address.invalid_street")]
    [InlineData("key", "Ada Lovelace", "AB-000001", Password, "Bishkek", "Chuy", null, "address.invalid_house")]
    public async Task Handle_WithInvalidRequest_DoesNotHashOrPersist(
        string? idempotencyKey,
        string? fullName,
        string? accountNumber,
        string? password,
        string? locality,
        string? street,
        string? house,
        string expectedCode)
    {
        var repository = new RecordingRepository(
            ResidentCreationPersistenceOutcome.Created);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(
            new CreateResidentCommand(
                idempotencyKey,
                fullName,
                accountNumber,
                password,
                locality,
                street,
                house,
                null,
                null,
                "director-user-id",
                "trace-id"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Null(passwordHasher.ReceivedPassword);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public void Command_ToStringRedactsAllInput()
    {
        var text = CreateCommand().ToString();

        Assert.DoesNotContain("create-resident-1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ada Lovelace", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AB-000001", text, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, text, StringComparison.Ordinal);
        Assert.DoesNotContain("Bishkek", text, StringComparison.Ordinal);
        Assert.DoesNotContain("director-user-id", text, StringComparison.Ordinal);
        Assert.DoesNotContain("trace-id", text, StringComparison.Ordinal);
        Assert.Contains("REDACTED", text, StringComparison.Ordinal);
    }

    private static CreateResidentCommand CreateCommand() =>
        new(
            "create-resident-1",
            "  Ada Lovelace  ",
            "  AB-000001  ",
            Password,
            "  Bishkek  ",
            " Chuy   Avenue ",
            " 42 ",
            " 2 ",
            " 17 ",
            "director-user-id",
            "trace-id");

    private static CreateResidentHandler CreateHandler(
        RecordingRepository repository,
        RecordingPasswordHasher passwordHasher) =>
        new(
            repository,
            new StubFingerprinter(),
            passwordHasher,
            PasswordPolicy.Default,
            new FixedTimeProvider(UtcNow));

    private sealed class RecordingRepository(
        ResidentCreationPersistenceOutcome outcome,
        ResidentId? residentId = null,
        AccountId? accountId = null,
        AddressId? addressId = null,
        ResidentCreationOperationId? operationId = null)
        : IResidentCreationRepository
    {
        public int CallCount { get; private set; }
        public UserAccount UserAccount { get; private set; } = null!;
        public Resident Resident { get; private set; } = null!;
        public Address Address { get; private set; } = null!;
        public Account Account { get; private set; } = null!;
        public ResidentCreationOperation Operation { get; private set; } = null!;
        public string ActorId { get; private set; } = string.Empty;
        public string CorrelationId { get; private set; } = string.Empty;

        public Task<ResidentCreationPersistenceResult> CreateAsync(
            UserAccount userAccount,
            Resident resident,
            Address address,
            Account account,
            ResidentCreationOperation operation,
            string actorId,
            string correlationId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            UserAccount = userAccount;
            Resident = resident;
            Address = address;
            Account = account;
            Operation = operation;
            ActorId = actorId;
            CorrelationId = correlationId;
            return Task.FromResult(
                new ResidentCreationPersistenceResult(
                    outcome,
                    residentId,
                    accountId,
                    addressId,
                    operationId));
        }
    }

    private sealed class StubFingerprinter : IResidentCreationRequestFingerprinter
    {
        public string Create(
            string fullName,
            string normalizedAccountNumber,
            string password,
            Address address) =>
            "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
    }

    private sealed class RecordingPasswordHasher : IPasswordHasher
    {
        public string? ReceivedPassword { get; private set; }

        public string Hash(string password)
        {
            ReceivedPassword = password;
            return "stored-password-hash";
        }

        public PasswordVerificationOutcome Verify(string password, string passwordHash) =>
            throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

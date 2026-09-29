using EcoBilling.Modules.Payments.Domain;
using EcoBilling.Modules.Payments.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Payments.Features.RegisterManual;

public sealed class RegisterManualPaymentHandler
{
    private readonly IPaymentRepository repository;
    private readonly TimeProvider timeProvider;

    public RegisterManualPaymentHandler(
        IPaymentRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<RegisterManualPaymentResult>> Handle(
        RegisterManualPaymentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.AccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var payment = Payment.Create(
            new PaymentId(Guid.NewGuid()),
            command.AccountId,
            command.Amount,
            command.IdempotencyKey,
            command.PaidAt,
            timeProvider.GetUtcNow());
        if (payment.IsFailure)
        {
            return Result<RegisterManualPaymentResult>.Failure(payment.Error);
        }

        var persistence = await repository.RegisterAsync(
            payment.Value,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        if (persistence.Payment is not null &&
            persistence.Outcome is PaymentRegistrationPersistenceOutcome.Registered or
                PaymentRegistrationPersistenceOutcome.Replayed)
        {
            return Result<RegisterManualPaymentResult>.Success(
                new RegisterManualPaymentResult(
                    persistence.Payment.Id.Value,
                    persistence.Payment.AccountId.Value,
                    persistence.Payment.Amount.Value,
                    persistence.Payment.PaidAt,
                    persistence.Overpayment,
                    persistence.IsReplay));
        }

        return persistence.Outcome switch
        {
            PaymentRegistrationPersistenceOutcome.AccountNotFound =>
                Result<RegisterManualPaymentResult>.Failure(PaymentErrors.AccountNotFound),
            PaymentRegistrationPersistenceOutcome.IdempotencyConflict =>
                Result<RegisterManualPaymentResult>.Failure(
                    PaymentErrors.IdempotencyConflict),
            _ => throw new InvalidOperationException(
                $"Unexpected payment registration outcome: {persistence.Outcome}.")
        };
    }
}

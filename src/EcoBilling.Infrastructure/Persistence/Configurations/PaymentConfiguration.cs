using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable(
            "payments",
            "payments",
            table => table.HasCheckConstraint(
                "ck_payments_amount_positive",
                "amount > 0"));

        builder.HasKey(payment => payment.Id)
            .HasName("pk_payments");

        builder.Property(payment => payment.Id)
            .HasColumnName("id")
            .HasConversion(
                paymentId => paymentId.Value,
                value => new PaymentId(value))
            .ValueGeneratedNever();

        builder.Property(payment => payment.AccountId)
            .HasColumnName("account_id")
            .HasConversion(
                accountId => accountId.Value,
                value => new AccountId(value))
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric")
            .HasConversion(
                amount => amount.Value,
                value => PaymentAmount.Create(value).Value)
            .IsRequired();

        builder.Property(payment => payment.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("text")
            .HasConversion(
                key => key.Value,
                value => PaymentIdempotencyKey.Create(value).Value)
            .IsRequired();

        builder.Property(payment => payment.PaidAt)
            .HasColumnName("paid_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(payment => payment.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(payment => payment.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_payments_idempotency_key");

        builder.HasIndex(payment => payment.AccountId)
            .HasDatabaseName("ix_payments_account_id");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(payment => payment.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payments_accounts_account_id");
    }
}

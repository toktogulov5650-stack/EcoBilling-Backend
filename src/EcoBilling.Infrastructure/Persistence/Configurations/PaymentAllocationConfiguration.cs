using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class PaymentAllocationConfiguration
    : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable(
            "payment_allocations",
            "payments",
            table => table.HasCheckConstraint(
                "ck_payment_allocations_amount_positive",
                "amount > 0"));

        builder.HasKey(allocation => allocation.Id)
            .HasName("pk_payment_allocations");

        builder.Property(allocation => allocation.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new PaymentAllocationId(value))
            .ValueGeneratedNever();

        builder.Property(allocation => allocation.PaymentId)
            .HasColumnName("payment_id")
            .HasConversion(
                id => id.Value,
                value => new PaymentId(value))
            .IsRequired();

        builder.Property(allocation => allocation.ChargeId)
            .HasColumnName("charge_id")
            .HasConversion(
                id => id.Value,
                value => new ChargeId(value))
            .IsRequired();

        builder.Property(allocation => allocation.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(allocation => allocation.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(allocation => new
            {
                allocation.PaymentId,
                allocation.ChargeId
            })
            .IsUnique()
            .HasDatabaseName(
                "ux_payment_allocations_payment_id_charge_id");

        builder.HasIndex(allocation => allocation.ChargeId)
            .HasDatabaseName("ix_payment_allocations_charge_id");

        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(allocation => allocation.PaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payment_allocations_payments_payment_id");

        builder.HasOne<Charge>()
            .WithMany()
            .HasForeignKey(allocation => allocation.ChargeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payment_allocations_charges_charge_id");
    }
}

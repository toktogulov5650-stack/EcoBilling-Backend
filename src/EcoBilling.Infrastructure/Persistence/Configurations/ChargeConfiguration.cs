using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> builder)
    {
        builder.ToTable(
            "charges",
            "billing",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_charges_billing_period",
                    "period_end > period_start");
                table.HasCheckConstraint(
                    "ck_charges_consumption_non_negative",
                    "consumption >= 0");
            });

        builder.HasKey(charge => charge.Id)
            .HasName("pk_charges");

        builder.Property(charge => charge.Id)
            .HasColumnName("id")
            .HasConversion(
                chargeId => chargeId.Value,
                value => new ChargeId(value))
            .ValueGeneratedNever();

        builder.Property(charge => charge.AccountId)
            .HasColumnName("account_id")
            .HasConversion(
                accountId => accountId.Value,
                value => new AccountId(value))
            .IsRequired();

        builder.Property(charge => charge.TariffVersionId)
            .HasColumnName("tariff_version_id")
            .HasConversion(
                versionId => versionId.Value,
                value => new TariffVersionId(value))
            .IsRequired();

        builder.Property(charge => charge.PeriodStart)
            .HasColumnName("period_start")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(charge => charge.PeriodEnd)
            .HasColumnName("period_end")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(charge => charge.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric")
            .IsRequired();

        builder.Property(charge => charge.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(charge => charge.PreviousReadingId)
            .HasColumnName("previous_reading_id")
            .HasConversion(
                readingId => readingId == null ? (Guid?)null : readingId.Value,
                value => value == null ? null : new MeterReadingId(value.Value));

        builder.Property(charge => charge.CurrentReadingId)
            .HasColumnName("current_reading_id")
            .HasConversion(
                readingId => readingId == null ? (Guid?)null : readingId.Value,
                value => value == null ? null : new MeterReadingId(value.Value));

        builder.Property(charge => charge.Consumption)
            .HasColumnName("consumption")
            .HasColumnType("numeric(18,3)")
            .IsRequired();

        builder.Property(charge => charge.CalculationVersion)
            .HasColumnName("calculation_version")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(charge => charge.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.HasIndex(charge => new
            {
                charge.AccountId,
                charge.PeriodStart,
                charge.PeriodEnd
            })
            .IsUnique()
            .HasDatabaseName("ux_charges_account_id_period_start_period_end");

        builder.HasIndex(charge => charge.TariffVersionId)
            .HasDatabaseName("ix_charges_tariff_version_id");

        builder.HasIndex(charge => charge.PreviousReadingId)
            .HasDatabaseName("ix_charges_previous_reading_id");

        builder.HasIndex(charge => charge.CurrentReadingId)
            .HasDatabaseName("ix_charges_current_reading_id");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(charge => charge.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_charges_accounts_account_id");

        builder.HasOne<TariffVersion>()
            .WithMany()
            .HasForeignKey(charge => charge.TariffVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_charges_tariff_versions_tariff_version_id");

        builder.HasOne<MeterReading>()
            .WithMany()
            .HasForeignKey(charge => charge.PreviousReadingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_charges_readings_previous_reading_id");

        builder.HasOne<MeterReading>()
            .WithMany()
            .HasForeignKey(charge => charge.CurrentReadingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_charges_readings_current_reading_id");
    }
}

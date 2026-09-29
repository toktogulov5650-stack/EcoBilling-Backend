using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Meters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class MeterConfiguration : IEntityTypeConfiguration<Meter>
{
    public void Configure(EntityTypeBuilder<Meter> builder)
    {
        builder.ToTable("meters", "meters");

        builder.HasKey(meter => meter.Id)
            .HasName("pk_meters");

        builder.Property(meter => meter.Id)
            .HasColumnName("id")
            .HasConversion(
                meterId => meterId.Value,
                value => new MeterId(value))
            .ValueGeneratedNever();

        builder.Property(meter => meter.AccountId)
            .HasColumnName("account_id")
            .HasConversion(
                accountId => accountId.Value,
                value => new AccountId(value))
            .IsRequired();

        builder.Property(meter => meter.SerialNumber)
            .HasColumnName("serial_number")
            .HasColumnType("text")
            .HasConversion(
                serialNumber => serialNumber.Value,
                value => MeterSerialNumber.Create(value).Value)
            .IsRequired();

        builder.Property(meter => meter.InstalledAt)
            .HasColumnName("installed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(meter => meter.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(meter => meter.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(meter => meter.RetiredAt)
            .HasColumnName("retired_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(meter => meter.ReplacesMeterId)
            .HasColumnName("replaces_meter_id")
            .HasConversion(
                meterId => meterId == null ? (Guid?)null : meterId.Value,
                value => value == null ? null : new MeterId(value.Value));

        builder.HasIndex(meter => meter.AccountId)
            .HasDatabaseName("ix_meters_account_id");

        builder.HasIndex(meter => meter.SerialNumber)
            .IsUnique()
            .HasDatabaseName("ux_meters_serial_number");

        builder.HasIndex(meter => meter.ReplacesMeterId)
            .IsUnique()
            .HasFilter("replaces_meter_id IS NOT NULL")
            .HasDatabaseName("ux_meters_replaces_meter_id");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(meter => meter.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_meters_accounts_account_id");

        builder.HasOne<Meter>()
            .WithMany()
            .HasForeignKey(meter => meter.ReplacesMeterId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_meters_meters_replaces_meter_id");
    }
}

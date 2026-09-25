using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Readings.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class MeterReadingConfiguration
    : IEntityTypeConfiguration<MeterReading>
{
    public void Configure(EntityTypeBuilder<MeterReading> builder)
    {
        builder.ToTable(
            "meter_readings",
            "readings",
            table => table.HasCheckConstraint(
                "ck_meter_readings_value_non_negative",
                "value >= 0"));

        builder.HasKey(reading => reading.Id)
            .HasName("pk_meter_readings");

        builder.Property(reading => reading.Id)
            .HasColumnName("id")
            .HasConversion(
                readingId => readingId.Value,
                value => new MeterReadingId(value))
            .ValueGeneratedNever();

        builder.Property(reading => reading.MeterId)
            .HasColumnName("meter_id")
            .HasConversion(
                meterId => meterId.Value,
                value => new MeterId(value))
            .IsRequired();

        builder.Property(reading => reading.Value)
            .HasColumnName("value")
            .HasColumnType("numeric")
            .HasConversion(
                readingValue => readingValue.Value,
                value => ReadingValue.Create(value).Value)
            .IsRequired();

        builder.Property(reading => reading.MeasuredAt)
            .HasColumnName("measured_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(reading => reading.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(reading => new { reading.MeterId, reading.MeasuredAt })
            .HasDatabaseName("ix_meter_readings_meter_id_measured_at");

        builder.HasOne<Meter>()
            .WithMany()
            .HasForeignKey(reading => reading.MeterId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_meter_readings_meters_meter_id");
    }
}

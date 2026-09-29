using EcoBilling.Modules.Identity.Domain;
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
            table =>
            {
                table.HasCheckConstraint(
                    "ck_meter_readings_value_non_negative",
                    "value >= 0");
                table.HasCheckConstraint(
                    "ck_meter_readings_source",
                    "source IN (1, 2, 3)");
            });

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

        builder.Property(reading => reading.AuthorUserId)
            .HasColumnName("author_user_id")
            .HasConversion(
                userId => userId == null ? (Guid?)null : userId.Value,
                value => value == null ? null : new UserId(value.Value));

        builder.Property(reading => reading.Source)
            .HasColumnName("source")
            .HasConversion<int>()
            .HasDefaultValue(ReadingSource.Import)
            .IsRequired();

        builder.Property(reading => reading.SupersedesReadingId)
            .HasColumnName("supersedes_reading_id")
            .HasConversion(
                readingId => readingId == null ? (Guid?)null : readingId.Value,
                value => value == null ? null : new MeterReadingId(value.Value));

        builder.Property(reading => reading.CorrectionReason)
            .HasColumnName("correction_reason")
            .HasMaxLength(MeterReading.MaximumCorrectionReasonLength);

        builder.HasIndex(reading => new { reading.MeterId, reading.MeasuredAt })
            .HasDatabaseName("ix_meter_readings_meter_id_measured_at");

        builder.HasIndex(reading => reading.AuthorUserId)
            .HasDatabaseName("ix_meter_readings_author_user_id");

        builder.HasIndex(reading => reading.SupersedesReadingId)
            .IsUnique()
            .HasFilter("supersedes_reading_id IS NOT NULL")
            .HasDatabaseName("ux_meter_readings_supersedes_reading_id");

        builder.HasOne<Meter>()
            .WithMany()
            .HasForeignKey(reading => reading.MeterId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_meter_readings_meters_meter_id");

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(reading => reading.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_meter_readings_user_accounts_author_user_id");

        builder.HasOne<MeterReading>()
            .WithMany()
            .HasForeignKey(reading => reading.SupersedesReadingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_meter_readings_meter_readings_supersedes_reading_id");
    }
}

using EcoBilling.Modules.Tariffs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class TariffVersionConfiguration
    : IEntityTypeConfiguration<TariffVersion>
{
    public void Configure(EntityTypeBuilder<TariffVersion> builder)
    {
        builder.ToTable(
            "tariff_versions",
            "tariffs",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_tariff_versions_rate_non_negative",
                    "rate >= 0");
                table.HasCheckConstraint(
                    "ck_tariff_versions_effective_period",
                    "effective_to IS NULL OR effective_to > effective_from");
            });

        builder.HasKey(version => version.Id)
            .HasName("pk_tariff_versions");

        builder.Property(version => version.Id)
            .HasColumnName("id")
            .HasConversion(
                versionId => versionId.Value,
                value => new TariffVersionId(value))
            .ValueGeneratedNever();

        builder.Property(version => version.TariffId)
            .HasColumnName("tariff_id")
            .HasConversion(
                tariffId => tariffId.Value,
                value => new TariffId(value))
            .IsRequired();

        builder.Property(version => version.Rate)
            .HasColumnName("rate")
            .HasColumnType("numeric")
            .HasConversion(
                rate => rate.Value,
                value => TariffRate.Create(value).Value)
            .IsRequired();

        builder.Property(version => version.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(version => version.EffectiveTo)
            .HasColumnName("effective_to")
            .HasColumnType("date");

        builder.Property(version => version.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(version => new { version.TariffId, version.EffectiveFrom })
            .HasDatabaseName("ix_tariff_versions_tariff_id_effective_from");

        builder.HasOne<Tariff>()
            .WithMany()
            .HasForeignKey(version => version.TariffId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tariff_versions_tariffs_tariff_id");
    }
}

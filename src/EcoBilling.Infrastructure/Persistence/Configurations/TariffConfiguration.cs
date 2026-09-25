using EcoBilling.Modules.Tariffs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.ToTable("tariffs", "tariffs");

        builder.HasKey(tariff => tariff.Id)
            .HasName("pk_tariffs");

        builder.Property(tariff => tariff.Id)
            .HasColumnName("id")
            .HasConversion(
                tariffId => tariffId.Value,
                value => new TariffId(value))
            .ValueGeneratedNever();

        builder.Property(tariff => tariff.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .HasConversion(
                tariffName => tariffName.Value,
                value => TariffName.Create(value).Value)
            .IsRequired();

        builder.Property(tariff => tariff.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}

using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class ResidentConfiguration : IEntityTypeConfiguration<Resident>
{
    public void Configure(EntityTypeBuilder<Resident> builder)
    {
        builder.ToTable("residents", "residents");

        builder.HasKey(resident => resident.Id)
            .HasName("pk_residents");

        builder.Property(resident => resident.Id)
            .HasColumnName("id")
            .HasConversion(
                residentId => residentId.Value,
                value => new ResidentId(value))
            .ValueGeneratedNever();

        builder.Property(resident => resident.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                userId => userId.Value,
                value => new UserId(value))
            .IsRequired();

        builder.Property(resident => resident.FullName)
            .HasColumnName("full_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(resident => resident.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(resident => resident.UserId)
            .IsUnique()
            .HasDatabaseName("ux_residents_user_id");

        builder.HasOne<UserAccount>()
            .WithOne()
            .HasForeignKey<Resident>(resident => resident.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_residents_user_accounts_user_id");
    }
}

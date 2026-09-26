using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class DirectorProfileConfiguration
    : IEntityTypeConfiguration<DirectorProfile>
{
    public void Configure(EntityTypeBuilder<DirectorProfile> builder)
    {
        builder.ToTable(
            "directors",
            "identity",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_directors_instance_slot",
                "instance_slot = 1"));

        builder.HasKey(director => director.Id)
            .HasName("pk_directors");

        builder.Property(director => director.Id)
            .HasColumnName("id")
            .HasConversion(
                directorId => directorId.Value,
                value => new DirectorId(value))
            .ValueGeneratedNever();

        builder.Property(director => director.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                userId => userId.Value,
                value => new UserId(value))
            .IsRequired();

        builder.Property(director => director.FullName)
            .HasColumnName("full_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(director => director.Slot)
            .HasColumnName("instance_slot")
            .IsRequired();

        builder.Property(director => director.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(director => director.UserId)
            .IsUnique()
            .HasDatabaseName("ux_directors_user_id");

        builder.HasIndex(director => director.Slot)
            .IsUnique()
            .HasDatabaseName("ux_directors_instance_slot");

        builder.HasOne<UserAccount>()
            .WithOne()
            .HasForeignKey<DirectorProfile>(director => director.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_directors_user_accounts_user_id");
    }
}

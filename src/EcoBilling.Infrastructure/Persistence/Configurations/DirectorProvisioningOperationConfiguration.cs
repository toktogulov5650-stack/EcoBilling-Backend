using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class DirectorProvisioningOperationConfiguration
    : IEntityTypeConfiguration<DirectorProvisioningOperation>
{
    public void Configure(
        EntityTypeBuilder<DirectorProvisioningOperation> builder)
    {
        builder.ToTable("director_provisioning_operations", "identity");

        builder.HasKey(operation => operation.Id)
            .HasName("pk_director_provisioning_operations");

        builder.Property(operation => operation.Id)
            .HasColumnName("id")
            .HasConversion(
                operationId => operationId.Value,
                value => new DirectorProvisioningOperationId(value))
            .ValueGeneratedNever();

        builder.Property(operation => operation.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(DirectorProvisioningOperation.MaximumIdempotencyKeyLength)
            .IsRequired();

        builder.Property(operation => operation.RequestFingerprint)
            .HasColumnName("request_fingerprint")
            .HasMaxLength(DirectorProvisioningOperation.RequestFingerprintLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(operation => operation.DirectorId)
            .HasColumnName("director_id")
            .HasConversion(
                directorId => directorId.Value,
                value => new DirectorId(value))
            .IsRequired();

        builder.Property(operation => operation.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(operation => operation.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_director_provisioning_operations_idempotency_key");

        builder.HasIndex(operation => operation.DirectorId)
            .HasDatabaseName("ix_director_provisioning_operations_director_id");

        builder.HasOne<DirectorProfile>()
            .WithMany()
            .HasForeignKey(operation => operation.DirectorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_director_provisioning_operations_directors_director_id");
    }
}

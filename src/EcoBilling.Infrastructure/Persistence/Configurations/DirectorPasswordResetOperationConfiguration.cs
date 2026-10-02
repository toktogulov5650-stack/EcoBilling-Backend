using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class DirectorPasswordResetOperationConfiguration
    : IEntityTypeConfiguration<DirectorPasswordResetOperation>
{
    public void Configure(EntityTypeBuilder<DirectorPasswordResetOperation> builder)
    {
        builder.ToTable("director_password_reset_operations", "identity");

        builder.HasKey(operation => operation.Id)
            .HasName("pk_director_password_reset_operations");

        builder.Property(operation => operation.Id)
            .HasColumnName("id")
            .HasConversion(
                operationId => operationId.Value,
                value => new DirectorPasswordResetOperationId(value))
            .ValueGeneratedNever();

        builder.Property(operation => operation.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(DirectorPasswordResetOperation.MaximumIdempotencyKeyLength)
            .IsRequired();

        builder.Property(operation => operation.RequestFingerprint)
            .HasColumnName("request_fingerprint")
            .HasMaxLength(DirectorPasswordResetOperation.RequestFingerprintLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(operation => operation.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(DirectorPasswordResetOperation.MaximumNormalizedEmailLength)
            .IsRequired();

        builder.Property(operation => operation.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(operation => operation.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_director_password_reset_operations_idempotency_key");
    }
}

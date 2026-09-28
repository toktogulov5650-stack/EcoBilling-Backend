using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class ResidentPasswordResetOperationConfiguration
    : IEntityTypeConfiguration<ResidentPasswordResetOperation>
{
    public void Configure(
        EntityTypeBuilder<ResidentPasswordResetOperation> builder)
    {
        builder.ToTable("resident_password_reset_operations", "residents");

        builder.HasKey(operation => operation.Id)
            .HasName("pk_resident_password_reset_operations");

        builder.Property(operation => operation.Id)
            .HasColumnName("id")
            .HasConversion(
                operationId => operationId.Value,
                value => new ResidentPasswordResetOperationId(value))
            .ValueGeneratedNever();

        builder.Property(operation => operation.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(ResidentPasswordResetOperation.MaximumIdempotencyKeyLength)
            .IsRequired();

        builder.Property(operation => operation.RequestFingerprint)
            .HasColumnName("request_fingerprint")
            .HasMaxLength(ResidentPasswordResetOperation.RequestFingerprintLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(operation => operation.ResidentId)
            .HasColumnName("resident_id")
            .HasConversion(
                residentId => residentId.Value,
                value => new ResidentId(value))
            .IsRequired();

        builder.Property(operation => operation.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(operation => operation.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_resident_password_reset_operations_idempotency_key");
        builder.HasIndex(operation => operation.ResidentId)
            .HasDatabaseName("ix_resident_password_reset_operations_resident_id");

        builder.HasOne<Resident>()
            .WithMany()
            .HasForeignKey(operation => operation.ResidentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_resident_password_reset_operations_residents_resident_id");
    }
}

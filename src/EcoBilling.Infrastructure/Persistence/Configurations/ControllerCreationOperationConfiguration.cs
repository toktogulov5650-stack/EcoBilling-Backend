using EcoBilling.Modules.Controllers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class ControllerCreationOperationConfiguration
    : IEntityTypeConfiguration<ControllerCreationOperation>
{
    public void Configure(
        EntityTypeBuilder<ControllerCreationOperation> builder)
    {
        builder.ToTable("controller_creation_operations", "controllers");

        builder.HasKey(operation => operation.Id)
            .HasName("pk_controller_creation_operations");

        builder.Property(operation => operation.Id)
            .HasColumnName("id")
            .HasConversion(
                operationId => operationId.Value,
                value => new ControllerCreationOperationId(value))
            .ValueGeneratedNever();

        builder.Property(operation => operation.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(ControllerCreationOperation.MaximumIdempotencyKeyLength)
            .IsRequired();

        builder.Property(operation => operation.RequestFingerprint)
            .HasColumnName("request_fingerprint")
            .HasMaxLength(ControllerCreationOperation.RequestFingerprintLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(operation => operation.ControllerId)
            .HasColumnName("controller_id")
            .HasConversion(
                controllerId => controllerId.Value,
                value => new ControllerId(value))
            .IsRequired();

        builder.Property(operation => operation.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(operation => operation.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_controller_creation_operations_idempotency_key");

        builder.HasIndex(operation => operation.ControllerId)
            .IsUnique()
            .HasDatabaseName("ux_controller_creation_operations_controller_id");

        builder.HasOne<Controller>()
            .WithOne()
            .HasForeignKey<ControllerCreationOperation>(operation => operation.ControllerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_controller_creation_operations_controllers_controller_id");
    }
}

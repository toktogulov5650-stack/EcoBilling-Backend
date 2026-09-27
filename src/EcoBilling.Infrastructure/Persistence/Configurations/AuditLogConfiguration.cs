using EcoBilling.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "infrastructure");

        builder.HasKey(log => log.Id)
            .HasName("pk_audit_logs");

        builder.Property(log => log.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(log => log.ActorType)
            .HasColumnName("actor_type")
            .HasMaxLength(AuditLog.MaximumActorTypeLength)
            .IsRequired();

        builder.Property(log => log.ActorId)
            .HasColumnName("actor_id")
            .HasMaxLength(AuditLog.MaximumActorIdLength)
            .IsRequired();

        builder.Property(log => log.Action)
            .HasColumnName("action")
            .HasMaxLength(AuditLog.MaximumActionLength)
            .IsRequired();

        builder.Property(log => log.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(AuditLog.MaximumEntityTypeLength)
            .IsRequired();

        builder.Property(log => log.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(AuditLog.MaximumEntityIdLength)
            .IsRequired();

        builder.Property(log => log.BeforeData)
            .HasColumnName("before_data")
            .HasColumnType("jsonb");

        builder.Property(log => log.AfterData)
            .HasColumnName("after_data")
            .HasColumnType("jsonb");

        builder.Property(log => log.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(AuditLog.MaximumCorrelationIdLength)
            .IsRequired();

        builder.Property(log => log.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(log => log.CreatedAt)
            .HasDatabaseName("ix_audit_logs_created_at");

        builder.HasIndex(log => new { log.EntityType, log.EntityId, log.CreatedAt })
            .HasDatabaseName("ix_audit_logs_entity_type_entity_id_created_at");

        builder.HasIndex(log => log.CorrelationId)
            .HasDatabaseName("ix_audit_logs_correlation_id");
    }
}

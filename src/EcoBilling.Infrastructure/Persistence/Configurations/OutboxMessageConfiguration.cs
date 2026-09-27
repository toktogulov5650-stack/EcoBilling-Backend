using EcoBilling.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(
            "outbox_messages",
            "infrastructure",
            table => table.HasCheckConstraint(
                "ck_outbox_messages_retry_count_non_negative",
                "retry_count >= 0"));

        builder.HasKey(message => message.Id)
            .HasName("pk_outbox_messages");

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.Type)
            .HasColumnName("type")
            .HasMaxLength(OutboxMessage.MaximumTypeLength)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(message => message.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(message => message.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(message => message.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(OutboxMessage.MaximumLastErrorLength);

        builder.HasIndex(message => new { message.ProcessedAt, message.OccurredAt })
            .HasDatabaseName("ix_outbox_messages_processed_at_occurred_at");
    }
}

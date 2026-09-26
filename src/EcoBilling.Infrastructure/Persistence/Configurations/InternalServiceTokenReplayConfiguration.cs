using EcoBilling.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class InternalServiceTokenReplayConfiguration
    : IEntityTypeConfiguration<InternalServiceTokenReplay>
{
    public void Configure(EntityTypeBuilder<InternalServiceTokenReplay> builder)
    {
        builder.ToTable("internal_service_token_replays", "infrastructure");

        builder.HasKey(replay => new { replay.Issuer, replay.TokenId })
            .HasName("pk_internal_service_token_replays");

        builder.Property(replay => replay.Issuer)
            .HasColumnName("issuer")
            .HasMaxLength(InternalServiceTokenReplay.MaximumIssuerLength);

        builder.Property(replay => replay.TokenId)
            .HasColumnName("token_id")
            .HasMaxLength(InternalServiceTokenReplay.MaximumTokenIdLength);

        builder.Property(replay => replay.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(replay => replay.ExpiresAt)
            .HasDatabaseName("ix_internal_service_token_replays_expires_at");
    }
}

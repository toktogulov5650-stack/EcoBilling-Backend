using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.ToTable(
            "refresh_sessions",
            "identity",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_refresh_sessions_expiry",
                    "expires_at > created_at");
                tableBuilder.HasCheckConstraint(
                    "ck_refresh_sessions_token_hash",
                    "token_hash ~ '^[0-9A-F]{64}$'");
            });

        builder.HasKey(session => session.Id)
            .HasName("pk_refresh_sessions");

        builder.Property(session => session.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new RefreshSessionId(value))
            .ValueGeneratedNever();

        builder.Property(session => session.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .IsRequired();

        builder.Property(session => session.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(session => session.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("character(64)")
            .IsFixedLength()
            .HasMaxLength(RefreshSession.TokenHashLength)
            .IsRequired();

        builder.Property(session => session.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(session => session.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(session => session.ConsumedAt)
            .HasColumnName("consumed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(session => session.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(session => session.ReplacedBySessionId)
            .HasColumnName("replaced_by_session_id")
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new RefreshSessionId(value.Value));

        builder.HasIndex(session => session.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_sessions_token_hash");
        builder.HasIndex(session => session.FamilyId)
            .HasDatabaseName("ix_refresh_sessions_family_id");
        builder.HasIndex(session => session.ExpiresAt)
            .HasDatabaseName("ix_refresh_sessions_expires_at");
        builder.HasIndex(session => session.UserId)
            .HasDatabaseName("ix_refresh_sessions_user_id");
        builder.HasIndex(session => session.ReplacedBySessionId)
            .IsUnique()
            .HasDatabaseName("ux_refresh_sessions_replaced_by_session_id");

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_refresh_sessions_user_accounts_user_id");

        builder.HasOne<RefreshSession>()
            .WithOne()
            .HasForeignKey<RefreshSession>(session => session.ReplacedBySessionId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_refresh_sessions_replacement");
    }
}

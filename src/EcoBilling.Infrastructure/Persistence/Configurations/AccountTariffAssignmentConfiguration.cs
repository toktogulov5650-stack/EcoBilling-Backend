using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class AccountTariffAssignmentConfiguration
    : IEntityTypeConfiguration<AccountTariffAssignment>
{
    public void Configure(EntityTypeBuilder<AccountTariffAssignment> builder)
    {
        builder.ToTable(
            "account_tariff_assignments",
            "tariffs",
            table => table.HasCheckConstraint(
                "ck_account_tariff_assignments_effective_period",
                "effective_to IS NULL OR effective_to > effective_from"));

        builder.HasKey(assignment => assignment.Id)
            .HasName("pk_account_tariff_assignments");

        builder.Property(assignment => assignment.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new AccountTariffAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(assignment => assignment.AccountId)
            .HasColumnName("account_id")
            .HasConversion(
                id => id.Value,
                value => new AccountId(value))
            .IsRequired();

        builder.Property(assignment => assignment.TariffId)
            .HasColumnName("tariff_id")
            .HasConversion(
                id => id.Value,
                value => new TariffId(value))
            .IsRequired();

        builder.Property(assignment => assignment.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(assignment => assignment.EffectiveTo)
            .HasColumnName("effective_to")
            .HasColumnType("date");

        builder.Property(assignment => assignment.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(assignment => new
            {
                assignment.AccountId,
                assignment.EffectiveFrom
            })
            .HasDatabaseName(
                "ix_account_tariff_assignments_account_id_effective_from");

        builder.HasIndex(assignment => assignment.TariffId)
            .HasDatabaseName("ix_account_tariff_assignments_tariff_id");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_account_tariff_assignments_accounts_account_id");

        builder.HasOne<Tariff>()
            .WithMany()
            .HasForeignKey(assignment => assignment.TariffId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_account_tariff_assignments_tariffs_tariff_id");
    }
}

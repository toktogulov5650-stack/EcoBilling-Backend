using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", "accounts");

        builder.HasKey(account => account.Id)
            .HasName("pk_accounts");

        builder.Property(account => account.Id)
            .HasColumnName("id")
            .HasConversion(
                accountId => accountId.Value,
                value => new AccountId(value))
            .ValueGeneratedNever();

        builder.Property(account => account.ResidentId)
            .HasColumnName("resident_id")
            .HasConversion(
                residentId => residentId.Value,
                value => new ResidentId(value))
            .IsRequired();

        builder.Property(account => account.AddressId)
            .HasColumnName("address_id")
            .HasConversion(
                addressId => addressId.Value,
                value => new AddressId(value))
            .IsRequired();

        builder.Property(account => account.Number)
            .HasColumnName("account_number")
            .HasColumnType("text")
            .HasConversion(
                accountNumber => accountNumber.Value,
                value => AccountNumber.Create(value).Value)
            .IsRequired();

        builder.Property(account => account.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(account => account.Number)
            .IsUnique()
            .HasDatabaseName("ux_accounts_account_number");

        builder.HasIndex(account => account.ResidentId)
            .HasDatabaseName("ix_accounts_resident_id");

        builder.HasIndex(account => account.AddressId)
            .HasDatabaseName("ix_accounts_address_id");

        builder.HasOne<Resident>()
            .WithMany()
            .HasForeignKey(account => account.ResidentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_accounts_residents_resident_id");

        builder.HasOne<Address>()
            .WithMany()
            .HasForeignKey(account => account.AddressId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_accounts_addresses_address_id");
    }
}

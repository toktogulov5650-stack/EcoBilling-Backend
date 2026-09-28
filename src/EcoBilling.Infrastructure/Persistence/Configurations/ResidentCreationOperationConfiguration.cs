using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class ResidentCreationOperationConfiguration
    : IEntityTypeConfiguration<ResidentCreationOperation>
{
    public void Configure(EntityTypeBuilder<ResidentCreationOperation> builder)
    {
        builder.ToTable("resident_creation_operations", "residents");

        builder.HasKey(operation => operation.Id)
            .HasName("pk_resident_creation_operations");

        builder.Property(operation => operation.Id)
            .HasColumnName("id")
            .HasConversion(
                operationId => operationId.Value,
                value => new ResidentCreationOperationId(value))
            .ValueGeneratedNever();

        builder.Property(operation => operation.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(ResidentCreationOperation.MaximumIdempotencyKeyLength)
            .IsRequired();

        builder.Property(operation => operation.RequestFingerprint)
            .HasColumnName("request_fingerprint")
            .HasMaxLength(ResidentCreationOperation.RequestFingerprintLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(operation => operation.ResidentId)
            .HasColumnName("resident_id")
            .HasConversion(
                residentId => residentId.Value,
                value => new ResidentId(value))
            .IsRequired();

        builder.Property(operation => operation.AccountId)
            .HasColumnName("account_id")
            .HasConversion(
                accountId => accountId.Value,
                value => new AccountId(value))
            .IsRequired();

        builder.Property(operation => operation.AddressId)
            .HasColumnName("address_id")
            .HasConversion(
                addressId => addressId.Value,
                value => new AddressId(value))
            .IsRequired();

        builder.Property(operation => operation.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(operation => operation.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_resident_creation_operations_idempotency_key");
        builder.HasIndex(operation => operation.ResidentId)
            .IsUnique()
            .HasDatabaseName("ux_resident_creation_operations_resident_id");
        builder.HasIndex(operation => operation.AccountId)
            .IsUnique()
            .HasDatabaseName("ux_resident_creation_operations_account_id");
        builder.HasIndex(operation => operation.AddressId)
            .IsUnique()
            .HasDatabaseName("ux_resident_creation_operations_address_id");

        builder.HasOne<Resident>()
            .WithOne()
            .HasForeignKey<ResidentCreationOperation>(operation => operation.ResidentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_resident_creation_operations_residents_resident_id");
        builder.HasOne<Account>()
            .WithOne()
            .HasForeignKey<ResidentCreationOperation>(operation => operation.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_resident_creation_operations_accounts_account_id");
        builder.HasOne<Address>()
            .WithOne()
            .HasForeignKey<ResidentCreationOperation>(operation => operation.AddressId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_resident_creation_operations_addresses_address_id");
    }

}

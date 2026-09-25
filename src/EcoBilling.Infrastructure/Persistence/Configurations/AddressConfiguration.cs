using EcoBilling.Modules.Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses", "accounts");

        builder.HasKey(address => address.Id)
            .HasName("pk_addresses");

        builder.Property(address => address.Id)
            .HasColumnName("id")
            .HasConversion(
                addressId => addressId.Value,
                value => new AddressId(value))
            .ValueGeneratedNever();

        builder.Property(address => address.Locality)
            .HasColumnName("locality")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(address => address.Street)
            .HasColumnName("street")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(address => address.House)
            .HasColumnName("house")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(address => address.Building)
            .HasColumnName("building")
            .HasColumnType("text");

        builder.Property(address => address.Apartment)
            .HasColumnName("apartment")
            .HasColumnType("text");

        builder.Property(address => address.SearchText)
            .HasColumnName("search_text")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(address => address.SearchText)
            .HasDatabaseName("ix_addresses_search_text");
    }
}

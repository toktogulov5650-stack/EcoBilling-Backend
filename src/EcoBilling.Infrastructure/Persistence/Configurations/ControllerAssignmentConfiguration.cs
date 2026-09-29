using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Controllers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class ControllerAssignmentConfiguration
    : IEntityTypeConfiguration<ControllerAssignment>
{
    public void Configure(EntityTypeBuilder<ControllerAssignment> builder)
    {
        builder.ToTable("controller_assignments", "controllers");

        builder.HasKey(assignment => assignment.Id)
            .HasName("pk_controller_assignments");

        builder.Property(assignment => assignment.Id)
            .HasColumnName("id")
            .HasConversion(
                assignmentId => assignmentId.Value,
                value => new ControllerAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(assignment => assignment.ControllerId)
            .HasColumnName("controller_id")
            .HasConversion(
                controllerId => controllerId.Value,
                value => new ControllerId(value))
            .IsRequired();

        builder.Property(assignment => assignment.AddressId)
            .HasColumnName("address_id")
            .HasConversion(
                addressId => addressId.Value,
                value => new AddressId(value))
            .IsRequired();

        builder.Property(assignment => assignment.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(assignment => new
            {
                assignment.ControllerId,
                assignment.AddressId
            })
            .IsUnique()
            .HasDatabaseName(
                "ux_controller_assignments_controller_id_address_id");

        builder.HasIndex(assignment => assignment.AddressId)
            .HasDatabaseName("ix_controller_assignments_address_id");

        builder.HasOne<Controller>()
            .WithMany()
            .HasForeignKey(assignment => assignment.ControllerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_controller_assignments_controllers_controller_id");

        builder.HasOne<Address>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AddressId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_controller_assignments_addresses_address_id");
    }
}

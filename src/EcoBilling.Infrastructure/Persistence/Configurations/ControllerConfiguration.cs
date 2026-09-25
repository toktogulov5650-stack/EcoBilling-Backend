using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class ControllerConfiguration : IEntityTypeConfiguration<Controller>
{
    public void Configure(EntityTypeBuilder<Controller> builder)
    {
        builder.ToTable("controllers", "controllers");

        builder.HasKey(controller => controller.Id)
            .HasName("pk_controllers");

        builder.Property(controller => controller.Id)
            .HasColumnName("id")
            .HasConversion(
                controllerId => controllerId.Value,
                value => new ControllerId(value))
            .ValueGeneratedNever();

        builder.Property(controller => controller.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                userId => userId.Value,
                value => new UserId(value))
            .IsRequired();

        builder.Property(controller => controller.FullName)
            .HasColumnName("full_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(controller => controller.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(controller => controller.UserId)
            .IsUnique()
            .HasDatabaseName("ux_controllers_user_id");

        builder.HasOne<UserAccount>()
            .WithOne()
            .HasForeignKey<Controller>(controller => controller.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_controllers_user_accounts_user_id");
    }
}

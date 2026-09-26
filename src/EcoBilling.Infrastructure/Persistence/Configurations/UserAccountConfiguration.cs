using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoBilling.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable(
            "user_accounts",
            "identity",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_user_accounts_login_type",
                    "login_type IN (1, 2)");
                tableBuilder.HasCheckConstraint(
                    "ck_user_accounts_role",
                    "role IN (1, 2, 3)");
            });

        builder.HasKey(userAccount => userAccount.Id)
            .HasName("pk_user_accounts");

        builder.Property(userAccount => userAccount.Id)
            .HasColumnName("id")
            .HasConversion(
                userId => userId.Value,
                value => new UserId(value))
            .ValueGeneratedNever();

        builder.Property(userAccount => userAccount.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(userAccount => userAccount.Role)
            .HasColumnName("role")
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(userAccount => userAccount.Role)
            .HasFilter("\"role\" = 3")
            .IsUnique()
            .HasDatabaseName("ux_user_accounts_single_director");

        builder.Property(userAccount => userAccount.RequiresPasswordChange)
            .HasColumnName("requires_password_change")
            .IsRequired();

        builder.Property(userAccount => userAccount.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.OwnsOne(
            userAccount => userAccount.LoginIdentity,
            ownedBuilder =>
            {
                ownedBuilder.Property(loginIdentity => loginIdentity.Type)
                    .HasColumnName("login_type")
                    .HasConversion<int>()
                    .IsRequired();

                ownedBuilder.Property(loginIdentity => loginIdentity.NormalizedValue)
                    .HasColumnName("normalized_login")
                    .HasColumnType("text")
                    .IsRequired();

                ownedBuilder.HasIndex(
                        loginIdentity => new
                        {
                            loginIdentity.Type,
                            loginIdentity.NormalizedValue
                        })
                    .IsUnique()
                    .HasDatabaseName("ux_user_accounts_login_type_normalized_login");
            });

        builder.Navigation(userAccount => userAccount.LoginIdentity)
            .IsRequired();
    }
}

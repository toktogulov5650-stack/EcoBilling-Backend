using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GrantDirectorPasswordResetRuntimeAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ecobilling_runtime') THEN
                        GRANT SELECT, INSERT, UPDATE, DELETE
                        ON TABLE identity.director_password_reset_operations
                        TO ecobilling_runtime;
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ecobilling_runtime') THEN
                        REVOKE SELECT, INSERT, UPDATE, DELETE
                        ON TABLE identity.director_password_reset_operations
                        FROM ecobilling_runtime;
                    END IF;
                END
                $$;
                """);
        }
    }
}

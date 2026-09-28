using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityLockoutState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_login_attempts",
                schema: "identity",
                table: "user_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end",
                schema: "identity",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_accounts_failed_login_attempts",
                schema: "identity",
                table: "user_accounts",
                sql: "failed_login_attempts >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_user_accounts_failed_login_attempts",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "failed_login_attempts",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "lockout_end",
                schema: "identity",
                table: "user_accounts");
        }
    }
}

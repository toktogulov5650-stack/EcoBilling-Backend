using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTariffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tariffs");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "tariffs",
                schema: "tariffs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tariffs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tariff_versions",
                schema: "tariffs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tariff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tariff_versions", x => x.id);
                    table.CheckConstraint("ck_tariff_versions_effective_period", "effective_to IS NULL OR effective_to > effective_from");
                    table.CheckConstraint("ck_tariff_versions_rate_non_negative", "rate >= 0");
                    table.ForeignKey(
                        name: "fk_tariff_versions_tariffs_tariff_id",
                        column: x => x.tariff_id,
                        principalSchema: "tariffs",
                        principalTable: "tariffs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tariff_versions_tariff_id_effective_from",
                schema: "tariffs",
                table: "tariff_versions",
                columns: new[] { "tariff_id", "effective_from" });

            migrationBuilder.Sql(
                """
                ALTER TABLE tariffs.tariff_versions
                ADD CONSTRAINT ex_tariff_versions_tariff_id_effective_period
                EXCLUDE USING gist
                (
                    tariff_id WITH =,
                    daterange(effective_from, effective_to, '[)') WITH &&
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tariff_versions",
                schema: "tariffs");

            migrationBuilder.DropTable(
                name: "tariffs",
                schema: "tariffs");

            migrationBuilder.DropSchema(
                name: "tariffs");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");
        }
    }
}

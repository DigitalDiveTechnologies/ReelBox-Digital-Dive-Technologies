using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialReelSaver.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminOperationalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_error_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    detail = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_error_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_error_logs_correlation_id",
                table: "app_error_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_error_logs_created_at",
                table: "app_error_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_app_error_logs_level",
                table: "app_error_logs",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_category",
                table: "system_settings",
                column: "category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_error_logs");

            migrationBuilder.DropTable(
                name: "system_settings");
        }
    }
}

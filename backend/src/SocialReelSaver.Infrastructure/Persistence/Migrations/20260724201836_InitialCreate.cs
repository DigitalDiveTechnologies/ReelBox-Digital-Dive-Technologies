using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialReelSaver.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_url = table.Column<string>(type: "text", nullable: false),
                    normalized_url = table.Column<string>(type: "text", nullable: true),
                    platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValueSql: "'preparing'"),
                    title = table.Column<string>(type: "text", nullable: true),
                    thumbnail_storage_key = table.Column<string>(type: "text", nullable: true),
                    media_storage_key = table.Column<string>(type: "text", nullable: true),
                    mime_type = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    progress_percent = table.Column<short>(type: "smallint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    download_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    downloaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_items", x => x.id);
                    table.CheckConstraint("ck_media_items_progress_percent", "progress_percent IS NULL OR (progress_percent >= 0 AND progress_percent <= 100)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_media_items_user_id_created_at",
                table: "media_items",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_media_items_user_id_normalized_url",
                table: "media_items",
                columns: new[] { "user_id", "normalized_url" },
                unique: true,
                filter: "normalized_url IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_media_items_user_id_status",
                table: "media_items",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_items");
        }
    }
}

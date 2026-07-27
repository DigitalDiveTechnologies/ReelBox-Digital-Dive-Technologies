using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialReelSaver.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignMediaItemsSrsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_retry_at",
                table: "media_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_items_status_next_retry_at",
                table: "media_items",
                columns: new[] { "status", "next_retry_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_media_items_users_user_id",
                table: "media_items",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_media_items_users_user_id",
                table: "media_items");

            migrationBuilder.DropIndex(
                name: "ix_media_items_status_next_retry_at",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "next_retry_at",
                table: "media_items");
        }
    }
}

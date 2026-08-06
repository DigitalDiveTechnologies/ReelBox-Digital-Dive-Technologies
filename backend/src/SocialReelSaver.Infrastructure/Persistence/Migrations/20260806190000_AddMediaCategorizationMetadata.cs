using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SocialReelSaver.Infrastructure.Persistence;

#nullable disable

namespace SocialReelSaver.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260806190000_AddMediaCategorizationMetadata")]
public partial class AddMediaCategorizationMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "description",
            table: "media_items",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "creator_username",
            table: "media_items",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "metadata_text",
            table: "media_items",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "description", table: "media_items");
        migrationBuilder.DropColumn(name: "creator_username", table: "media_items");
        migrationBuilder.DropColumn(name: "metadata_text", table: "media_items");
    }
}

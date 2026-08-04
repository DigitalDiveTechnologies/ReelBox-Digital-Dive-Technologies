using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialReelSaver.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaItemCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "media_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "media_items");
        }
    }
}

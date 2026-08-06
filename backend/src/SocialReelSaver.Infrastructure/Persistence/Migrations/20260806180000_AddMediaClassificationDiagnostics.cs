using SocialReelSaver.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialReelSaver.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260806180000_AddMediaClassificationDiagnostics")]
public partial class AddMediaClassificationDiagnostics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "category_confidence",
            table: "media_items",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "classification_source",
            table: "media_items",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "category_confidence",
            table: "media_items");

        migrationBuilder.DropColumn(
            name: "classification_source",
            table: "media_items");
    }
}

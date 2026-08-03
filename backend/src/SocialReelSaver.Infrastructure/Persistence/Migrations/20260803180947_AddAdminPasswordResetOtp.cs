using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialReelSaver.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPasswordResetOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_reset_otp_expires_at",
                table: "admin_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_otp_hash",
                table: "admin_users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_reset_otp_expires_at",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "password_reset_otp_hash",
                table: "admin_users");
        }
    }
}

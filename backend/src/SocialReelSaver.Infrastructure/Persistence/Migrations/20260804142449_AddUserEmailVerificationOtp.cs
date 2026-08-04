using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialReelSaver.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailVerificationOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_verification_otp_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email_verification_otp_hash",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "email_verified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_verification_otp_expires_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verification_otp_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verified",
                table: "users");
        }
    }
}

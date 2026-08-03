using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Infrastructure.Persistence.Configurations;

public sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("ix_admin_users_email");

        builder.Property(e => e.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256);

        builder.Property(e => e.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired()
            .HasDefaultValue(AdminRole.Analyst);

        builder.Property(e => e.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(128);

        builder.Property(e => e.RefreshTokenExpiresAt)
            .HasColumnName("refresh_token_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.PasswordResetOtpHash)
            .HasColumnName("password_reset_otp_hash")
            .HasMaxLength(512);

        builder.Property(e => e.PasswordResetOtpExpiresAt)
            .HasColumnName("password_reset_otp_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}

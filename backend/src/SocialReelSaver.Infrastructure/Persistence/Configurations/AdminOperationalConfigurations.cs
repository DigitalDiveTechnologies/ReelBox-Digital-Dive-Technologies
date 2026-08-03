using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Infrastructure.Persistence.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").HasColumnType("text").IsRequired();
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(64).IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedByAdminId).HasColumnName("updated_by_admin_id").HasColumnType("uuid");
        builder.HasIndex(e => e.Category).HasDatabaseName("ix_system_settings_category");
    }
}

public sealed class AppErrorLogConfiguration : IEntityTypeConfiguration<AppErrorLog>
{
    public void Configure(EntityTypeBuilder<AppErrorLog> builder)
    {
        builder.ToTable("app_error_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedOnAdd();
        builder.Property(e => e.Level).HasColumnName("level").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Message).HasColumnName("message").HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Detail).HasColumnName("detail").HasColumnType("text");
        builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(256);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        builder.Property(e => e.Path).HasColumnName("path").HasMaxLength(512);
        builder.Property(e => e.StatusCode).HasColumnName("status_code");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_app_error_logs_created_at");
        builder.HasIndex(e => e.Level).HasDatabaseName("ix_app_error_logs_level");
        builder.HasIndex(e => e.CorrelationId).HasDatabaseName("ix_app_error_logs_correlation_id");
    }
}

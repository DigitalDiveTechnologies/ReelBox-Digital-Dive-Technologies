using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.AdminId)
            .HasColumnName("admin_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(e => e.AdminEmail)
            .HasColumnName("admin_email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(128);

        builder.Property(e => e.OldValuesJson)
            .HasColumnName("old_values_json")
            .HasColumnType("text");

        builder.Property(e => e.NewValuesJson)
            .HasColumnName("new_values_json")
            .HasColumnType("text");

        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(64);

        builder.Property(e => e.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(64);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_audit_logs_created_at");
        builder.HasIndex(e => e.AdminId).HasDatabaseName("ix_audit_logs_admin_id");
        builder.HasIndex(e => e.Action).HasDatabaseName("ix_audit_logs_action");
    }
}

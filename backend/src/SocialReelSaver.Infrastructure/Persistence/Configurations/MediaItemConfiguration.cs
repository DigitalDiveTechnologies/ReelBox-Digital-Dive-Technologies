using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Infrastructure.Persistence.Configurations;

public sealed class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.ToTable("media_items");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(e => e.OriginalUrl)
            .HasColumnName("original_url")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.NormalizedUrl)
            .HasColumnName("normalized_url")
            .HasColumnType("text");

        builder.Property(e => e.Platform)
            .HasColumnName("platform")
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<MediaPlatform>(value, true))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<MediaStatus>(value, true))
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValueSql("'preparing'");

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasColumnType("text");

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasMaxLength(64);

        builder.Property(e => e.ThumbnailStorageKey)
            .HasColumnName("thumbnail_storage_key")
            .HasColumnType("text");

        builder.Property(e => e.MediaStorageKey)
            .HasColumnName("media_storage_key")
            .HasColumnType("text");

        builder.Property(e => e.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(127);

        builder.Property(e => e.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .HasColumnType("bigint");

        builder.Property(e => e.DurationMs)
            .HasColumnName("duration_ms")
            .HasColumnType("bigint");

        builder.Property(e => e.ProgressPercent)
            .HasColumnName("progress_percent")
            .HasColumnType("smallint");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.DownloadStartedAt)
            .HasColumnName("download_started_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.DownloadedAt)
            .HasColumnName("downloaded_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.NextRetryAt)
            .HasColumnName("next_retry_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.ErrorCode)
            .HasColumnName("error_code")
            .HasMaxLength(64);

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");

        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .HasColumnType("integer")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(e => e.User)
            .WithMany(u => u.MediaItems)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_media_items_progress_percent",
                "progress_percent IS NULL OR (progress_percent >= 0 AND progress_percent <= 100)");
        });

        // SRS §12.2 — library queries by owner + newest first
        builder.HasIndex(e => new { e.UserId, e.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_media_items_user_id_created_at");

        // SRS §12.2 — pending/failed filters
        builder.HasIndex(e => new { e.UserId, e.Status })
            .HasDatabaseName("ix_media_items_user_id_status");

        // SRS §12.2 — optional unique/partial duplicate policy on (user_id, normalized_url)
        builder.HasIndex(e => new { e.UserId, e.NormalizedUrl })
            .IsUnique()
            .HasFilter("normalized_url IS NOT NULL")
            .HasDatabaseName("ix_media_items_user_id_normalized_url");

        // SRS §12.2 — worker DB polling support (status / next_retry_at)
        builder.HasIndex(e => new { e.Status, e.NextRetryAt })
            .HasDatabaseName("ix_media_items_status_next_retry_at");
    }
}

using Microsoft.EntityFrameworkCore;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Infrastructure.Persistence;

namespace SocialReelSaver.Tests.Persistence;

/// <summary>
/// Verifies SRS §12 media_items / users model wiring (entities, FK, indexes).
/// </summary>
public sealed class DatabaseSchemaTests
{
    [Fact]
    public void Model_ExposesSrsMediaItemColumnsAndIndexes()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(MediaItem));
        Assert.NotNull(entity);

        Assert.Equal("media_items", entity!.GetTableName());
        Assert.NotNull(entity.FindProperty(nameof(MediaItem.NextRetryAt)));
        Assert.NotNull(entity.FindProperty(nameof(MediaItem.NormalizedUrl)));
        Assert.NotNull(entity.FindProperty(nameof(MediaItem.RetryCount)));

        var fk = entity.GetForeignKeys().Single();
        Assert.Equal(typeof(User), fk.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior);

        var indexNames = entity.GetIndexes().Select(i => i.GetDatabaseName()).ToHashSet();
        Assert.Contains("ix_media_items_user_id_created_at", indexNames);
        Assert.Contains("ix_media_items_user_id_status", indexNames);
        Assert.Contains("ix_media_items_user_id_normalized_url", indexNames);
        Assert.Contains("ix_media_items_status_next_retry_at", indexNames);
    }

    [Fact]
    public async Task MediaItem_PersistsWithOwnerRelationship()
    {
        await using var db = CreateContext();
        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "schema@test.local",
            PasswordHash = "hash",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var media = new MediaItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OriginalUrl = "https://www.instagram.com/reel/SCHEMA1/",
            NormalizedUrl = "https://www.instagram.com/reel/SCHEMA1/",
            Platform = MediaPlatform.Instagram,
            Status = MediaStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5),
            RetryCount = 0,
        };

        db.Users.Add(user);
        db.MediaItems.Add(media);
        await db.SaveChangesAsync();

        var loaded = await db.MediaItems
            .Include(m => m.User)
            .SingleAsync(m => m.Id == media.Id);

        Assert.Equal(user.Id, loaded.UserId);
        Assert.NotNull(loaded.User);
        Assert.Equal(user.Email, loaded.User!.Email);
        Assert.NotNull(loaded.NextRetryAt);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"schema-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

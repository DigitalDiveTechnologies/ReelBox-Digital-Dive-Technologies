using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Playback;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Infrastructure.Playback;
using SocialReelSaver.Infrastructure.Storage;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Tests.Worker;

public sealed class ObjectStorageAndPlaybackTests
{
    [Fact]
    public async Task LocalStorage_UploadReplaceValidateDelete_Lifecycle()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b9-storage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sut = new LocalObjectStorageService(
                Options.Create(new ObjectStorageOptions { LocalRootPath = root }),
                NullLogger<LocalObjectStorageService>.Instance);

            var key = "user/media/clip.mp4";
            await using (var content = new MemoryStream("video-bytes-001"u8.ToArray()))
            {
                var upload = await sut.UploadAsync(new StorageUploadRequest
                {
                    Key = key,
                    Content = content,
                    ContentType = "video/mp4",
                    ContentLength = content.Length,
                });
                Assert.True(upload.Success);
                Assert.NotNull(upload.Metadata);
            }

            var validation = await sut.ValidateAsync(key, "video/mp4", "video-bytes-001"u8.Length);
            Assert.True(validation.Success);

            await using (var replaceContent = new MemoryStream("video-bytes-0022"u8.ToArray()))
            {
                var replace = await sut.ReplaceAsync(new StorageUploadRequest
                {
                    Key = key,
                    Content = replaceContent,
                    ContentType = "video/mp4",
                });
                Assert.True(replace.Success);
            }

            var meta = await sut.GetMetadataAsync(key);
            Assert.NotNull(meta);
            Assert.Equal("video/mp4", meta!.ContentType);

            var health = await sut.CheckHealthAsync();
            Assert.True(health.Healthy);
            Assert.True(health.Available);

            var delete = await sut.DeleteAsync(key);
            Assert.True(delete.Success);
            Assert.True(delete.Deleted);
            Assert.False(await sut.ExistsAsync(key));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LocalSignedUrl_CreateAndValidate_RoundTrip()
    {
        var storageOptions = Options.Create(new ObjectStorageOptions
        {
            PublicApiBaseUrl = "http://localhost:5080",
            PlaybackUrlExpirationMinutes = 10,
            PlaybackSigningKey = "PLAYBACK_TEST_SIGNING_KEY_AT_LEAST_32_CHARS!!",
        });
        var jwt = Options.Create(new JwtOptions { SigningKey = "JWT_FALLBACK_UNUSED_IN_THIS_TEST_KEY!!!!" });
        var sut = new LocalSignedUrlProvider(storageOptions, jwt);

        var mediaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var key = "abc/media.mp4";

        var created = await sut.CreatePlaybackUrlAsync(new SignedUrlRequest
        {
            MediaId = mediaId,
            UserId = userId,
            StorageKey = key,
        });

        Assert.True(created.Success);
        Assert.Equal("application_signed_url", created.Delivery);
        Assert.Contains("/api/v1/media/", created.Url);
        Assert.Contains("sig=", created.Url);

        var query = new Uri(created.Url!).Query.TrimStart('?')
            .Split('&')
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

        Assert.True(sut.TryValidatePlaybackToken(
            mediaId,
            userId,
            key,
            long.Parse(query["exp"]),
            query["sig"]));
    }

    [Fact]
    public async Task PlaybackUrlService_RequiresCompletedOwnedMediaWithStoredObject()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b9-play-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var storageOptions = Options.Create(new ObjectStorageOptions
            {
                LocalRootPath = root,
                PublicApiBaseUrl = "http://localhost:5080",
                PlaybackSigningKey = "PLAYBACK_TEST_SIGNING_KEY_AT_LEAST_32_CHARS!!",
            });
            var storage = new LocalObjectStorageService(storageOptions, NullLogger<LocalObjectStorageService>.Instance);
            var key = "u1/m1/media.mp4";
            await using (var stream = new MemoryStream(new byte[32]))
            {
                await storage.UploadAsync(new StorageUploadRequest
                {
                    Key = key,
                    Content = stream,
                    ContentType = "video/mp4",
                });
            }

            var item = new MediaItem
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                OriginalUrl = "https://instagram.com/reel/x",
                Platform = MediaPlatform.Instagram,
                Status = MediaStatus.Completed,
                MediaStorageKey = key,
                MimeType = "video/mp4",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var signedUrls = new LocalSignedUrlProvider(storageOptions, Options.Create(new JwtOptions
            {
                SigningKey = "JWT_FALLBACK_UNUSED_IN_THIS_TEST_KEY!!!!",
            }));
            var service = new PlaybackUrlService(
                new PlaybackAuthorization(),
                signedUrls,
                storage,
                new MediaThumbnailUrlService(signedUrls, storageOptions),
                storageOptions);

            var metadata = await service.CreateAsync(item, item.UserId);
            Assert.Equal("application_signed_url", metadata.Delivery);
            Assert.False(string.IsNullOrWhiteSpace(metadata.PlaybackUrl));

            Assert.Throws<BadRequestException>(() =>
            {
                var incomplete = new MediaItem
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    OriginalUrl = item.OriginalUrl,
                    Platform = item.Platform,
                    Status = MediaStatus.Queued,
                    MediaStorageKey = item.MediaStorageKey,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                };
                new PlaybackAuthorization().EnsureCanRequestPlayback(item.UserId, incomplete);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

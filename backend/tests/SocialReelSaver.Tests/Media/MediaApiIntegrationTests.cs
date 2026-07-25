using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Infrastructure.Persistence;
using SocialReelSaver.Tests.Authentication;

namespace SocialReelSaver.Tests.Media;

public sealed class MediaApiIntegrationTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public MediaApiIntegrationTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MediaEndpoints_RequireAuthentication()
    {
        var response = await _client.GetAsync("/api/v1/media");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_List_Get_Duplicate_Retry_Delete_Flow()
    {
        await AuthenticateAsync();

        var create = await _client.PostAsJsonAsync(
            "/api/v1/media",
            new CreateMediaRequest("https://www.instagram.com/reel/TESTFLOW1/", "share_sheet"));

        Assert.Equal(HttpStatusCode.Accepted, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<MediaResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("instagram", created.Platform);
        Assert.Equal("queued", created.Status);

        var duplicate = await _client.PostAsJsonAsync(
            "/api/v1/media",
            new CreateMediaRequest("https://instagram.com/reel/TESTFLOW1/?utm_source=x", "manual"));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        var reused = await duplicate.Content.ReadFromJsonAsync<MediaResponse>(JsonOptions);
        Assert.NotNull(reused);
        Assert.Equal(created.Id, reused.Id);

        var list = await _client.GetAsync("/api/v1/media?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var page = await list.Content.ReadFromJsonAsync<MediaListResponse>(JsonOptions);
        Assert.NotNull(page);
        Assert.Contains(page.Items, i => i.Id == created.Id);

        var get = await _client.GetAsync($"/api/v1/media/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        // Force failed state for retry rules.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.MediaItems.FindAsync(created.Id);
            Assert.NotNull(entity);
            entity!.Status = MediaStatus.Failed;
            entity.ErrorCode = "PROVIDER_TEMPORARY_FAILURE";
            entity.ErrorMessage = "temporary";
            await db.SaveChangesAsync();
        }

        var retryCompletedGuard = await _client.PostAsync($"/api/v1/media/{created.Id}/retry", null);
        Assert.Equal(HttpStatusCode.OK, retryCompletedGuard.StatusCode);
        var retried = await retryCompletedGuard.Content.ReadFromJsonAsync<MediaResponse>(JsonOptions);
        Assert.NotNull(retried);
        Assert.Equal("queued", retried.Status);
        Assert.Equal(1, retried.RetryCount);

        // Completed cannot be retried.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
            var entity = await db.MediaItems.FindAsync(created.Id);
            Assert.NotNull(entity);

            const string key = "media/test.mp4";
            await using (var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 }))
            {
                var upload = await storage.ReplaceAsync(new StorageUploadRequest
                {
                    Key = key,
                    Content = stream,
                    ContentType = "video/mp4",
                });
                Assert.True(upload.Success);
            }

            entity!.Status = MediaStatus.Completed;
            entity.MediaStorageKey = key;
            entity.MimeType = "video/mp4";
            entity.FileSizeBytes = 4;
            await db.SaveChangesAsync();
        }

        var retryCompleted = await _client.PostAsync($"/api/v1/media/{created.Id}/retry", null);
        Assert.Equal(HttpStatusCode.BadRequest, retryCompleted.StatusCode);

        var playback = await _client.GetAsync($"/api/v1/media/{created.Id}/playback");
        Assert.Equal(HttpStatusCode.OK, playback.StatusCode);
        var playbackBody = await playback.Content.ReadFromJsonAsync<PlaybackResponse>(JsonOptions);
        Assert.NotNull(playbackBody);
        Assert.Equal("application_signed_url", playbackBody.Delivery);
        Assert.False(string.IsNullOrWhiteSpace(playbackBody.PlaybackUrl));
        Assert.Contains("/content?", playbackBody.PlaybackUrl);

        var delete = await _client.DeleteAsync($"/api/v1/media/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var missing = await _client.GetAsync($"/api/v1/media/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Create_UnsupportedPlatform_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/v1/media",
            new CreateMediaRequest("https://tiktok.com/@u/video/1", "manual"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotAccessAnotherUsersMedia()
    {
        await AuthenticateAsync("owner_");
        var create = await _client.PostAsJsonAsync(
            "/api/v1/media",
            new CreateMediaRequest("https://www.facebook.com/watch/?v=999", "manual"));
        Assert.Equal(HttpStatusCode.Accepted, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<MediaResponse>(JsonOptions);
        Assert.NotNull(created);

        // Switch to another user.
        _client.DefaultRequestHeaders.Authorization = null;
        await AuthenticateAsync("intruder_");

        var get = await _client.GetAsync($"/api/v1/media/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    private async Task AuthenticateAsync(string emailPrefix = "media_")
    {
        var email = $"{emailPrefix}{Guid.NewGuid():N}@example.com";
        const string password = "SecurePass1";

        var register = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Tokens.AccessToken);
    }
}

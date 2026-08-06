using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SocialReelSaver.Application.Abstractions.Email;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Infrastructure.Persistence;

namespace SocialReelSaver.Tests.Authentication;

/// <summary>
/// Captures signup OTP from outbound mail so integration tests can complete verify-email.
/// </summary>
public sealed class CapturingEmailService : IEmailService
{
    private static readonly Regex OtpPattern = new(@"\b(\d{6})\b", RegexOptions.Compiled);

    public string? LastOtp { get; private set; }

    public Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken cancellationToken = default)
    {
        var source = plainTextBody ?? htmlBody;
        var match = OtpPattern.Match(source);
        LastOtp = match.Success ? match.Groups[1].Value : null;
        return Task.CompletedTask;
    }
}

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"auth-tests-{Guid.NewGuid()}";
    public CapturingEmailService Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Highest-priority overrides so appsettings Redis defaults do not force a live Redis in tests.
        builder.UseSetting("Worker:UseInMemoryQueue", "true");
        builder.UseSetting("Redis:ConnectionString", "");
        builder.UseSetting("ConnectionStrings:Redis", "");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "SocialReelSaver",
                ["Jwt:Audience"] = "SocialReelSaver.Mobile",
                ["Jwt:SigningKey"] = "TEST_ONLY_SIGNING_KEY_AT_LEAST_32_CHARS_LONG!!",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Database:ConnectionString"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Redis:ConnectionString"] = "",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = "",
                ["Worker:UseInMemoryQueue"] = "true",
                ["Worker:MaxRetries"] = "3",
                ["Worker:BaseBackoffSeconds"] = "1",
                ["Worker:MaxBackoffSeconds"] = "8",
                ["ObjectStorage:Provider"] = "Local",
                ["ObjectStorage:LocalRootPath"] = Path.Combine(Path.GetTempPath(), "srs-test-storage"),
                ["Download:TempFolder"] = Path.Combine(Path.GetTempPath(), "srs-test-temp"),
                ["Download:TimeoutSeconds"] = "30",
                ["Download:MaxFileSizeBytes"] = "1048576",
                ["Smtp:Host"] = "",
                ["Smtp:Password"] = "",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveDbContextRegistrations(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Email);
        });
    }

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var descriptors = services
            .Where(d =>
                d.ServiceType == typeof(AppDbContext)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                || d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>)
                || (d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                    && d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext)))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        services.RemoveAll(typeof(AppDbContext));
        services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
    }
}

public sealed class AuthApiIntegrationTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public AuthApiIntegrationTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<AuthResponse> RegisterAndVerifyAsync(string email, string password)
    {
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        Assert.False(string.IsNullOrWhiteSpace(_factory.Email.LastOtp));

        var verifyResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/verify-email",
            new VerifyEmailRequest(email, _factory.Email.LastOtp!));
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var auth = await verifyResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);
        return auth;
    }

    [Fact]
    public async Task Register_Login_Me_Refresh_Logout_Flow_Succeeds()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        const string password = "SecurePass1";

        var registered = await RegisterAndVerifyAsync(email, password);
        Assert.Equal(email, registered.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(registered.Tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(registered.Tokens.RefreshToken));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registered.Tokens.AccessToken);

        var meResponse = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(me);
        Assert.Equal(email, me.Email);

        _client.DefaultRequestHeaders.Authorization = null;
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshRequest(registered.Tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(refreshed);
        Assert.NotEqual(registered.Tokens.RefreshToken, refreshed.Tokens.RefreshToken);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshed.Tokens.AccessToken);

        var logoutResponse = await _client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var reuseRefresh = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshRequest(refreshed.Tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseRefresh.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_RemainsAnonymous()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        const string password = "SecurePass1";

        await RegisterAndVerifyAsync(email, password);

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var email = $"login_{Guid.NewGuid():N}@example.com";
        const string password = "SecurePass1";

        await RegisterAndVerifyAsync(email, password);

        var login = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "WrongPass1"));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }
}

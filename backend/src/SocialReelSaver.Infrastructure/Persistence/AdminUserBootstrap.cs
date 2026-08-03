using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Persistence;

/// <summary>
/// Seeds the first SuperAdmin when <c>admin_users</c> is empty and bootstrap credentials are configured.
/// </summary>
public static class AdminUserBootstrap
{
    public static async Task EnsureSeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AdminBootstrapOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AdminUserBootstrap");

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        if (options.Password.Length < 8)
        {
            logger.LogWarning("AdminBootstrap:Password must be at least 8 characters; seed skipped.");
            return;
        }

        var admins = scope.ServiceProvider.GetRequiredService<IAdminUserRepository>();
        var targetEmail = options.Email.Trim().ToLowerInvariant();
        var existing = await admins.GetByEmailAsync(targetEmail, cancellationToken);
        if (existing is null)
        {
            var oldLocal = await admins.GetByEmailAsync("admin@reelbox.local", cancellationToken);
            if (oldLocal is not null)
            {
                oldLocal.Email = targetEmail;
                oldLocal.UpdatedAt = DateTimeOffset.UtcNow;
                await admins.UpdateAsync(oldLocal, cancellationToken);
                await admins.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Updated bootstrap SuperAdmin email to {Email}", targetEmail);
                return;
            }
        }
        else
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var now = DateTimeOffset.UtcNow;
        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            Email = options.Email.Trim().ToLowerInvariant(),
            PasswordHash = hasher.HashPassword(options.Password),
            DisplayName = string.IsNullOrWhiteSpace(options.DisplayName)
                ? "Super Admin"
                : options.DisplayName.Trim(),
            Role = AdminRole.SuperAdmin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await admins.AddAsync(admin, cancellationToken);
        await admins.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded initial SuperAdmin {Email}", admin.Email);
    }
}

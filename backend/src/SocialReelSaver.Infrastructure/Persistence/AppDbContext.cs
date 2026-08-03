using Microsoft.EntityFrameworkCore;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<MediaItem> MediaItems => Set<MediaItem>();

    public DbSet<User> Users => Set<User>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<AppErrorLog> AppErrorLogs => Set<AppErrorLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

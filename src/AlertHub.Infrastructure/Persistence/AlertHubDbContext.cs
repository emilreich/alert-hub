using AlertHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Infrastructure.Persistence;

public class AlertHubDbContext(DbContextOptions<AlertHubDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserChannelConfig> UserChannelConfigs => Set<UserChannelConfig>();
    public DbSet<Outlet> Outlets => Set<Outlet>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<FeedEntry> FeedEntries => Set<FeedEntry>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlertHubDbContext).Assembly);
        SeedData.Apply(modelBuilder);
    }
}

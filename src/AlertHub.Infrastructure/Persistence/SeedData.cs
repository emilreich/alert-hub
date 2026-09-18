using AlertHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Infrastructure.Persistence;

/// <summary>
/// Static, fixed-ID reference data — deliberately deterministic (no randomness)
/// so tests and demos are reproducible. Actual seeded User accounts are added in
/// a later migration once password hashing exists (AuthService, M2) rather than
/// hardcoding a placeholder hash here.
/// </summary>
public static class SeedData
{
    public static class PermissionKeys
    {
        public const string SubscriptionsManageOwn = "subscriptions.manageOwn";
        public const string FeedViewOwn = "feed.viewOwn";
        public const string ArticlesViewAll = "articles.viewAll";
        public const string UsersManageAny = "users.manageAny";
    }

    public static class RoleNames
    {
        public const string User = "User";
        public const string Admin = "Admin";
    }

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Key = PermissionKeys.SubscriptionsManageOwn },
            new Permission { Id = 2, Key = PermissionKeys.FeedViewOwn },
            new Permission { Id = 3, Key = PermissionKeys.ArticlesViewAll },
            new Permission { Id = 4, Key = PermissionKeys.UsersManageAny }
        );

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = RoleNames.User },
            new Role { Id = 2, Name = RoleNames.Admin }
        );

        // User role -> subscriptions.manageOwn, feed.viewOwn
        // Admin role -> articles.viewAll, users.manageAny (no personal subscription/feed layer)
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = 1, PermissionId = 1 },
            new RolePermission { RoleId = 1, PermissionId = 2 },
            new RolePermission { RoleId = 2, PermissionId = 3 },
            new RolePermission { RoleId = 2, PermissionId = 4 }
        );

        modelBuilder.Entity<Outlet>().HasData(
            new Outlet { Id = 1, Name = "CNN" },
            new Outlet { Id = 2, Name = "Fox News" },
            new Outlet { Id = 3, Name = "ESPN" },
            new Outlet { Id = 4, Name = "Reuters" },
            new Outlet { Id = 5, Name = "Bloomberg" }
        );

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Breaking News" },
            new Category { Id = 2, Name = "Sports" },
            new Category { Id = 3, Name = "Politics" },
            new Category { Id = 4, Name = "Markets" },
            new Category { Id = 5, Name = "Natural Disasters" },
            new Category { Id = 6, Name = "Technology" }
        );
    }
}

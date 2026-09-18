using AlertHub.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        // Unsubscribe/resubscribe toggles IsActive on this same row rather than
        // delete + reinsert, so this unique pair holds regardless of active state.
        builder.HasIndex(s => new { s.UserId, s.OutletId, s.CategoryId }).IsUnique();

        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Outlet)
            .WithMany()
            .HasForeignKey(s => s.OutletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

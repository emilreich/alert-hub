namespace AlertHub.Core.Entities;

/// <summary>
/// The unit of subscription is a (User, Outlet, Category) pair, not two independent
/// lists — a user can trust one outlet for some categories and not others.
/// Unsubscribe/resubscribe toggles IsActive on the same row rather than delete +
/// reinsert, so history (when it was on/off) is preserved.
/// </summary>
public class Subscription
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int OutletId { get; set; }
    public Outlet Outlet { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

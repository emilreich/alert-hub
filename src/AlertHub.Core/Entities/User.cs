namespace AlertHub.Core.Entities;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Disabling freezes the user's feed and stops future notification dispatch
    /// (checked fresh per request, not baked into the JWT). Login stays allowed and
    /// existing feed history stays visible.
    /// </summary>
    public bool IsDisabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<UserChannelConfig> ChannelConfigs { get; set; } = new List<UserChannelConfig>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

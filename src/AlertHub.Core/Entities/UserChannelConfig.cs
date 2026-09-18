namespace AlertHub.Core.Entities;

/// <summary>
/// A user's destination for one notification channel (e.g. "email" -> an address,
/// "slack" -> a webhook URL). Generic by design so adding a channel type later is a
/// new ChannelKey value, not a schema change.
/// </summary>
public class UserChannelConfig
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public required string ChannelKey { get; set; }
    public required string Destination { get; set; }
}

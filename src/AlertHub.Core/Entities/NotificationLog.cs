using AlertHub.Core.Enums;

namespace AlertHub.Core.Entities;

/// <summary>
/// One row per (FeedEntry, channel) delivery attempt — a single matched article can
/// fan out to multiple channels the user has configured. Destination is a snapshot
/// of the channel config at send time, so it survives the user later changing their
/// configured destination.
/// </summary>
public class NotificationLog
{
    public int Id { get; set; }

    public int FeedEntryId { get; set; }
    public FeedEntry FeedEntry { get; set; } = null!;

    public required string ChannelKey { get; set; }
    public required string Destination { get; set; }
    public NotificationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}

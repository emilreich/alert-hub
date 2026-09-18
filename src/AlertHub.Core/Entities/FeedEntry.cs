namespace AlertHub.Core.Entities;

/// <summary>
/// Materialized, write-time record that an article matched a user's subscriptions —
/// deliberately NOT a live join over Article/Subscription. A live join would let a
/// later subscription change retroactively surface old articles, which was
/// explicitly rejected in favor of "future-only": a FeedEntry is only ever created
/// at the moment an article is categorized, against whatever subscriptions exist
/// then, so it can never be affected by a later subscription change.
/// </summary>
public class FeedEntry
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public DateTime MatchedAt { get; set; }

    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
}

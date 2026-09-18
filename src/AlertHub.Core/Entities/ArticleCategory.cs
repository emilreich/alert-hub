namespace AlertHub.Core.Entities;

/// <summary>
/// Join row: an article can carry multiple categories. Composite key
/// (ArticleId, CategoryId).
/// </summary>
public class ArticleCategory
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

using AlertHub.Core.Enums;

namespace AlertHub.Core.Entities;

/// <summary>
/// Deliberately lightweight: headline, outlet, source URL, categories — never the
/// full body, to keep storage flat regardless of catalog size. Full content is
/// served on demand by IArticleContentProvider, never persisted here.
/// </summary>
public class Article
{
    public int Id { get; set; }
    public required string Headline { get; set; }

    public int OutletId { get; set; }
    public Outlet Outlet { get; set; } = null!;

    public required string SourceUrl { get; set; }
    public DateTime PublishedAt { get; set; }

    public CategorizationStatus CategorizationStatus { get; set; } = CategorizationStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public ICollection<ArticleCategory> ArticleCategories { get; set; } = new List<ArticleCategory>();
}

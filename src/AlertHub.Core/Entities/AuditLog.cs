using AlertHub.Core.Enums;

namespace AlertHub.Core.Entities;

/// <summary>
/// Generic, single-table audit trail. ActorUserId vs TargetUserId gives the
/// admin/self-service distinction for free: a regular user can only ever act on
/// their own data (Actor == Target); only an admin can have Actor != Target. Adding
/// a new auditable action later is a new AuditAction value, not a new table.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    public int ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;

    public int TargetUserId { get; set; }
    public User TargetUser { get; set; } = null!;

    public AuditAction Action { get; set; }

    /// <summary>JSON snapshot of the specific before/after payload for this action.</summary>
    public required string Details { get; set; }

    public DateTime CreatedAt { get; set; }
}

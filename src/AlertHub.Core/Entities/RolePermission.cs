namespace AlertHub.Core.Entities;

/// <summary>
/// Join row: which permissions a role grants. Composite key (RoleId, PermissionId).
/// </summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

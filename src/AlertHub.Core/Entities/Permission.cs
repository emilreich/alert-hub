namespace AlertHub.Core.Entities;

public class Permission
{
    public int Id { get; set; }

    /// <summary>
    /// Stable key referenced by authorization policies, e.g. "subscriptions.manageOwn".
    /// </summary>
    public required string Key { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

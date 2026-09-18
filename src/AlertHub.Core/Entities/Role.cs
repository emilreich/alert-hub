namespace AlertHub.Core.Entities;

public class Role
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

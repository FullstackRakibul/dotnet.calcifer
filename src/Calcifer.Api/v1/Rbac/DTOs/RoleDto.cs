namespace Calcifer.Api.Rbac.DTOs
{
	public record RoleDto(
	string Id,
	string Name,
	string Description,
	bool IsSystem,
	int UsersCount,
	int PermissionCount,
	DateTime LastUpdated
);
}

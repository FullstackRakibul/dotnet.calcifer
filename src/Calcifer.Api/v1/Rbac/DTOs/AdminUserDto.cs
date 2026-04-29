namespace Calcifer.Api.Rbac.DTOs
{
	public record AdminUserDto(
	string Id, string FirstName, string LastName,
	string Email, string? Phone, string? Department,
	int? BaseUnitId, string? BaseUnitName,
	string Status,  // "active"|"inactive"|"pending"|"locked"
	DateTime JoinedDate, DateTime? LastLogin, string? AvatarUrl,
	List<UserUnitRoleDto> UnitRoles,
	List<DirectPermissionDto> DirectPermissions
);
}

using System.ComponentModel.DataAnnotations;

namespace Calcifer.Api.Rbac.DTOs
{
	// ════════════════════════════════════════════════════════════════════════
	//  PERMISSION DTOs
	// ════════════════════════════════════════════════════════════════════════

	public record PermissionDto(
		int Id,
		string Module,
		string Resource,
		string Action,
		string? Description
	)
	{
		/// <summary>Canonical key used in cache / JWT: "Module:Resource:Action"</summary>
		public string Key => $"{Module}:{Resource}:{Action}";
	}

	// ════════════════════════════════════════════════════════════════════════
	//  ROLE PERMISSION DTOs
	// ════════════════════════════════════════════════════════════════════════

	public record AssignRolePermissionRequest(
		[Required] int PermissionId
	);

	public record RolePermissionDto(
		string RoleId,
		string RoleName,
		int PermissionId,
		string Module,
		string Resource,
		string Action
	);

	// ════════════════════════════════════════════════════════════════════════
	//  USER UNIT ROLE DTOs
	// ════════════════════════════════════════════════════════════════════════

	public record AssignUnitRoleRequest(
		[Required] string RoleId,
		[Required] int UnitId,
		DateTime? ValidFrom,
		DateTime? ValidTo
	);

	public record RevokeUnitRoleRequest(
		[Required] string RoleId,
		[Required] int UnitId
	);

	public record UserUnitRoleDto(
		string UserId,
		string RoleId,
		string RoleName,
		int UnitId,
		string UnitName,
		DateTime? ValidFrom,
		DateTime? ValidTo,
		bool IsActive
	);

	// ════════════════════════════════════════════════════════════════════════
	//  DIRECT PERMISSION DTOs
	// ════════════════════════════════════════════════════════════════════════

	public record SetDirectPermissionRequest(
		[Required] int PermissionId,
		[Required] bool IsGranted,
		DateTime? ExpiresAt
	);

	public record DirectPermissionDto(
		string UserId,
		int PermissionId,
		string Module,
		string Resource,
		string Action,
		bool IsGranted,
		string? GrantedBy,
		DateTime? ExpiresAt
	);

	// ════════════════════════════════════════════════════════════════════════
	//  EFFECTIVE PERMISSION SUMMARY  (GET /rbac/users/{id}/permissions)
	// ════════════════════════════════════════════════════════════════════════

	public record UserPermissionSummary(
		string UserId,
		string UserName,
		string Email,
		List<string> Roles,
		List<string> EffectivePermissions,   // "Module:Resource:Action" keys
		List<DirectPermissionDto> DirectOverrides,
		DateTime CacheGeneratedAt,
		bool CacheIsStale            // true if cache is older than 5 min
	);

	// ════════════════════════════════════════════════════════════════════════
	//  ORG UNIT DTOs
	// ════════════════════════════════════════════════════════════════════════

	public record CreateOrgUnitRequest(
		[Required, MaxLength(100)] string Name,
		[MaxLength(50)] string? Code,
		[MaxLength(50)] string? Level,
		int? ParentId
	);

	public record OrgUnitDto(
		int Id,
		string Name,
		string? Code,
		string? Level,
		int? ParentId,
		List<OrgUnitDto> Children
	);
}
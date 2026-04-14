// ============================================================
//  RbacDTOs.cs — all DTOs for the RBAC module in one file.
// ============================================================

namespace Calcifer.Api.DbContexts.Rbac.DTOs
{
	// ── Outbound ─────────────────────────────────────────────────

	public class PermissionDto
	{
		public int Id { get; set; }
		public string Module { get; set; } = string.Empty;
		public string Resource { get; set; } = string.Empty;
		public string Action { get; set; } = string.Empty;
		public string? Description { get; set; }
		public string ClaimValue => $"{Module}:{Resource}:{Action}";
	}

	public class UserUnitRoleDto
	{
		public string RoleId { get; set; } = string.Empty;
		public string RoleName { get; set; } = string.Empty;
		public int UnitId { get; set; }
		public string UnitName { get; set; } = string.Empty;
		public DateTime ValidFrom { get; set; }
		public DateTime? ValidTo { get; set; }
		public bool IsExpired => ValidTo.HasValue && ValidTo < DateTime.UtcNow;
	}

	public class UserPermissionSummaryDto
	{
		public string UserId { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public IEnumerable<UserUnitRoleDto> UnitRoles { get; set; } = [];
		public IEnumerable<string> EffectivePermissions { get; set; } = [];
		public IEnumerable<DirectOverrideDto> DirectOverrides { get; set; } = [];
	}

	public class DirectOverrideDto
	{
		public int PermissionId { get; set; }
		public string ClaimValue { get; set; } = string.Empty;
		public bool IsGranted { get; set; }
		public DateTime? ExpiresAt { get; set; }
		public string? Reason { get; set; }
		public string? GrantedBy { get; set; }
	}

	// ── Inbound ──────────────────────────────────────────────────

	public class AssignUnitRoleRequest
	{
		public string UserId { get; set; } = string.Empty;
		public string RoleId { get; set; } = string.Empty;
		public int UnitId { get; set; }
		public DateTime? ValidTo { get; set; }
		public string? Notes { get; set; }
	}

	public class SetDirectPermissionRequest
	{
		public string UserId { get; set; } = string.Empty;
		public int PermissionId { get; set; }
		public bool IsGranted { get; set; } = true;
		public DateTime? ExpiresAt { get; set; }
		public string? Reason { get; set; }
	}

	public class AssignRolePermissionRequest
	{
		public string RoleId { get; set; } = string.Empty;
		public int PermissionId { get; set; }
	}
}

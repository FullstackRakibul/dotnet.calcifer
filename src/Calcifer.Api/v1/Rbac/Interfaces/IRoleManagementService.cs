// ============================================================
//  IRoleManagementService.cs
//
//  Isolation rule: this interface is the ONLY surface the rest
//  of the application depends on. Controllers, filters, and
//  minimal APIs import this interface — never RbacService directly.
//
//  Categories:
//    Roles CRUD · Role-Permissions · Permission Checks
// ============================================================

using Calcifer.Api.Rbac.DTOs;

namespace Calcifer.Api.Rbac.Interfaces
{
	public interface IRoleManagementService
	{
		// ── Roles CRUD ──────────────────────────────────────────────
		Task<List<RoleDto>> GetAllRolesAsync(CancellationToken ct = default);
		Task<RoleDto?> GetRoleByIdAsync(string id, CancellationToken ct = default);
		Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, string createdBy, CancellationToken ct = default);
		Task<RoleDto> UpdateRoleAsync(string id, UpdateRoleRequest request, string updatedBy, CancellationToken ct = default);
		Task<bool> DeleteRoleAsync(string id, string deletedBy, CancellationToken ct = default);

		// ── Role ↔ Permissions ──────────────────────────────────────
		Task<List<RolePermissionDto>> GetRolePermissionsAsync(string roleId, CancellationToken ct = default);
		Task<RolePermissionDto> AssignPermissionToRoleAsync(string roleId, int permissionId, string assignedBy, CancellationToken ct = default);
		Task<bool> RemovePermissionFromRoleAsync(string roleId, int permissionId, string removedBy, CancellationToken ct = default);

		/// <summary>Bulk sync — delete all existing, insert new set</summary>
		Task SyncRolePermissionsAsync(string roleId, int[] permissionIds, string updatedBy, CancellationToken ct = default);

		// ── Permission Checks ───────────────────────────────────────
		/// <summary>
		/// Check if user has a specific permission (async, DB-fresh).
		/// Used by RbacExtensions for service-layer permission enforcement.
		/// </summary>
		Task<bool> HasPermissionAsync(string userId, string module, string resource, string action, CancellationToken ct = default);

		/// <summary>
		/// Get all effective permissions for a user (role-based + direct overrides).
		/// Returns a set of "Module:Resource:Action" keys.
		/// </summary>
		Task<IReadOnlySet<string>> GetUserEffectivePermissionsAsync(string userId, CancellationToken ct = default);
	}
}
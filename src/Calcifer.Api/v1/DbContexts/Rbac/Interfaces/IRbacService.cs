// ============================================================
//  IRbacService.cs
//
//  Isolation rule: this interface is the ONLY surface the rest
//  of the application depends on. Controllers, filters, and
//  minimal APIs import this interface — never RbacService directly.
//
//  13 methods in 5 categories:
//    Resolution (3) · Cache (2) · Roles (3) · Direct (2) · RoleConfig (3)
// ============================================================

using Calcifer.Api.DTOs.RbacDTO;

namespace Calcifer.Api.Interface.Rbac
{
	public interface IRbacService
	{
		// ── Resolution ────────────────────────────────────────────────────

		Task<IReadOnlySet<string>> GetPermissionsAsync(
			string userId, CancellationToken ct = default);

		Task<bool> HasPermissionAsync(
			string userId, string module, string resource, string action,
			CancellationToken ct = default);

		Task<IEnumerable<string>> BuildJwtPermissionClaimsAsync(
			string userId, CancellationToken ct = default);

		// ── Cache ─────────────────────────────────────────────────────────

		/// <summary>Wipe the cache for a user. Next call to GetPermissionsAsync rebuilds it.</summary>
		Task InvalidateCacheAsync(string userId, CancellationToken ct = default);

		/// <summary>Force-rebuild the cache for a user immediately.</summary>
		Task RecomputeCacheAsync(string userId, CancellationToken ct = default);

		// ── User ↔ Role ↔ Unit assignments ───────────────────────────────

		/// <summary>
		/// Assign a user to a role scoped to an org unit.
		/// Also keeps ASP.NET Identity UserRoles in sync for JWT ClaimTypes.Role.
		/// Rebuilds PermissionCache on success.
		/// </summary>
		Task<(bool Success, string Message)> AssignUserToUnitRoleAsync(
			string userId, string roleId, int unitId,
			DateTime? validFrom = null, DateTime? validTo = null,
			string? assignedBy = null, CancellationToken ct = default);

		/// <summary>
		/// Soft-delete a user's role assignment at a unit.
		/// Rebuilds PermissionCache on success.
		/// </summary>
		Task<(bool Success, string Message)> RevokeUserUnitRoleAsync(
			string userId, string roleId, int unitId,
			string? revokedBy = null, CancellationToken ct = default);

		/// <summary>Returns all active unit-role assignments for a user.</summary>
		Task<IEnumerable<UserUnitRoleDto>> GetUserUnitRolesAsync(
			string userId, CancellationToken ct = default);

		// ── Direct permission overrides ───────────────────────────────────

		/// <summary>
		/// Upsert a direct permission override for a user.
		///   IsGranted = true  → explicit grant (even if role doesn't have it)
		///   IsGranted = false → explicit deny  (even if role does have it)
		/// Rebuilds PermissionCache on success.
		/// </summary>
		Task<(bool Success, string Message)> SetDirectPermissionAsync(
			string userId, int permissionId, bool isGranted,
			DateTime? expiresAt = null, string? grantedBy = null,
			CancellationToken ct = default);

		/// <summary>Remove a direct override, reverting to role-based access. Rebuilds cache.</summary>
		Task<(bool Success, string Message)> RemoveDirectPermissionAsync(
			string userId, int permissionId,
			string? removedBy = null, CancellationToken ct = default);

		// ── Role ↔ Permission configuration ──────────────────────────────

		/// <summary>Returns all permissions currently attached to a role.</summary>
		Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(
			string roleId, CancellationToken ct = default);

		/// <summary>
		/// Add a permission to a role.
		/// Rebuilds cache for all users who hold this role.
		/// </summary>
		Task<(bool Success, string Message)> AssignPermissionToRoleAsync(
			string roleId, int permissionId,
			string? assignedBy = null, CancellationToken ct = default);

		/// <summary>
		/// Remove a permission from a role.
		/// Rebuilds cache for all users who hold this role.
		/// </summary>
		Task<(bool Success, string Message)> RevokePermissionFromRoleAsync(
			string roleId, int permissionId,
			string? revokedBy = null, CancellationToken ct = default);
	}
}
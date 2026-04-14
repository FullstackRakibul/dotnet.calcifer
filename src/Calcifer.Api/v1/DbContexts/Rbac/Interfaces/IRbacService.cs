// ============================================================
//  IRbacService.cs
//  The RBAC engine contract. All permission logic goes through
//  here. Controllers and filters NEVER touch the DB directly.
//
//  Isolation rule: This interface (and its implementation) live
//  in the RBAC module only. No other service imports it except
//  through DI. The license system is completely separate.
// ============================================================

using Calcifer.Api.DbContexts.Rbac.Entities;
using Calcifer.Api.DbContexts.Rbac.DTOs;

namespace Calcifer.Api.DbContexts.Rbac.Interfaces
{
	public interface IRbacService
	{
		// ── Permission resolution ────────────────────────────────

		/// <summary>
		/// Returns the full resolved permission set for a user at a given unit.
		/// Applies role permissions + direct overrides.
		/// Result is cached in PermissionCache.
		/// </summary>
		Task<IReadOnlySet<string>> GetPermissionsAsync(string userId, int? unitId = null, CancellationToken ct = default);

		/// <summary>
		/// Fast check: does this user have this one permission?
		/// Uses cache — no joins at request time.
		/// </summary>
		Task<bool> HasPermissionAsync(string userId, string module, string resource, string action, int? unitId = null, CancellationToken ct = default);

		/// <summary>
		/// Builds the compact "perms" claim string list for JWT generation.
		/// Called once at login / token refresh.
		/// </summary>
		Task<IEnumerable<string>> BuildJwtPermissionClaimsAsync(string userId, CancellationToken ct = default);

		// ── Cache management ─────────────────────────────────────

		/// <summary>
		/// Invalidates the cache for a user (all units).
		/// Call this whenever roles or direct permissions change.
		/// </summary>
		Task InvalidateCacheAsync(string userId, CancellationToken ct = default);

		/// <summary>
		/// Recomputes and stores the cache for a user.
		/// </summary>
		Task RecomputeCacheAsync(string userId, CancellationToken ct = default);

		// ── Role management ──────────────────────────────────────

		/// <summary>
		/// Assigns a user to a role at a specific org unit.
		/// Enforces one-role-per-user-per-unit.
		/// </summary>
		Task AssignUserToUnitRoleAsync(string userId, string roleId, int unitId, DateTime? validTo = null, string? assignedBy = null, CancellationToken ct = default);

		/// <summary>
		/// Removes a user's role at a specific org unit.
		/// </summary>
		Task RevokeUserUnitRoleAsync(string userId, string roleId, int unitId, CancellationToken ct = default);

		/// <summary>
		/// Returns all active unit-role assignments for a user.
		/// </summary>
		Task<IEnumerable<UserUnitRoleDto>> GetUserUnitRolesAsync(string userId, CancellationToken ct = default);

		// ── Direct permission overrides ──────────────────────────

		/// <summary>
		/// Grants or denies a specific permission directly to a user.
		/// </summary>
		Task SetDirectPermissionAsync(string userId, int permissionId, bool isGranted, DateTime? expiresAt = null, string? reason = null, string? grantedBy = null, CancellationToken ct = default);

		/// <summary>
		/// Removes a direct permission override from a user.
		/// </summary>
		Task RemoveDirectPermissionAsync(string userId, int permissionId, CancellationToken ct = default);

		// ── Query helpers ────────────────────────────────────────

		/// <summary>
		/// Returns all permissions assigned to a role.
		/// </summary>
		Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(string roleId, CancellationToken ct = default);

		/// <summary>
		/// Assigns a permission to a role.
		/// </summary>
		Task AssignPermissionToRoleAsync(string roleId, int permissionId, string? assignedBy = null, CancellationToken ct = default);

		/// <summary>
		/// Removes a permission from a role.
		/// </summary>
		Task RevokePermissionFromRoleAsync(string roleId, int permissionId, CancellationToken ct = default);
	}
}
// ============================================================
//  RbacExtensions.cs
//
//  Extension methods for:
//    ClaimsPrincipal  — sync JWT-only permission checks
//    IRbacService     — async DB-backed checks with throw-on-fail
// ============================================================

using System.Security.Claims;
using Calcifer.Api.Rbac.Interfaces;

namespace Calcifer.Api.Rbac.Extensions
{
	// ════════════════════════════════════════════════════════════════════════
	//  ClaimsPrincipal extensions  —  synchronous, JWT-only
	// ════════════════════════════════════════════════════════════════════════
	public static class ClaimsPrincipalExtensions
	{
		/// <summary>
		/// Returns all "perms" claims from the JWT as an immutable set.
		/// Returns empty set if no perms claims are present.
		/// </summary>
		public static IReadOnlySet<string> GetPermissions(this ClaimsPrincipal user)
			=> user.FindAll("perms").Select(c => c.Value).ToHashSet();

		/// <summary>
		/// Synchronous JWT-only permission check.
		/// Use this in Razor / Blazor where async is inconvenient.
		/// Does NOT fall back to the DB — stale JWT = stale answer.
		/// </summary>
		public static bool HasPermission(
			this ClaimsPrincipal user,
			string module, string resource, string action)
		{
			var perms = user.GetPermissions();
			if (perms.Count == 0) return false;

			return perms.Contains($"{module}:{resource}:{action}")
				|| perms.Contains($"{module}:*:*")
				|| perms.Contains("*:*:*");
		}

		/// <summary>
		/// Returns the current user's ID claim ("ID").
		/// Returns null if the claim is absent (unauthenticated).
		/// </summary>
		public static string? GetUserId(this ClaimsPrincipal user)
			=> user.FindFirst("ID")?.Value;

		/// <summary>
		/// Returns true if the JWT user ID matches targetUserId.
		/// Use this to enforce "can only access own resource" rules (R-self).
		/// </summary>
		public static bool IsSelf(this ClaimsPrincipal user, string targetUserId)
			=> user.GetUserId() == targetUserId;
	}

	// ════════════════════════════════════════════════════════════════════════
	//  IRbacService extensions  —  async, always DB-fresh
	// ════════════════════════════════════════════════════════════════════════
	public static class RbacServiceExtensions
	{
		/// <summary>
		/// Same as HasPermissionAsync but throws UnauthorizedAccessException
		/// on failure instead of returning false.
		/// Useful in service-layer code where you want to fail loudly.
		/// </summary>
		public static async Task RequireAsync(
			this IRoleManagementService rbac,
			string userId, string module, string resource, string action,
			CancellationToken ct = default)
		{
			var allowed = await rbac.HasPermissionAsync(userId, module, resource, action, ct);
			if (!allowed)
				throw new UnauthorizedAccessException(
					$"User {userId} does not have permission {module}:{resource}:{action}");
		}

		/// <summary>
		/// Filters a collection to only items the user has the given permission for.
		/// The keySelector extracts the scoping value from each item (e.g. OrgUnitId).
		/// The permissionResolver maps each key to a specific permission key.
		///
		/// Useful for row-level filtering where different items need different checks.
		/// </summary>
		public static async Task<IEnumerable<T>> FilterByPermissionAsync<T>(
			this IRoleManagementService rbac,
			string userId,
			IEnumerable<T> items,
			Func<T, (string Module, string Resource, string Action)> permissionSelector,
			CancellationToken ct = default)
		{
			var allPerms = await rbac.SyncRolePermissionsAsync(userId, ct);
			return items.Where(item =>
			{
				var (module, resource, action) = permissionSelector(item);
				var key = $"{module}:{resource}:{action}";
				return allPerms.Contains(key)
					|| allPerms.Contains($"{module}:*:*")
					|| allPerms.Contains("*:*:*");
			});
		}
	}
}
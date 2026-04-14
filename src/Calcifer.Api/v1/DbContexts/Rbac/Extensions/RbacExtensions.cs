// ============================================================
//  RbacExtensions.cs
//  Extension methods that make permission checks ergonomic
//  throughout the codebase — controllers, services, and
//  Minimal API handlers all use these.
//
//  Usage examples:
//
//  // In a controller:
//  if (!User.HasPermission("HCM", "Employee", "Delete"))
//      return Forbid();
//
//  // In a Minimal API handler:
//  .WithMetadata(new RequirePermissionAttribute("Finance", "Payroll", "Export"))
//
//  // In a service (async DB-backed check):
//  await _rbac.RequireAsync(userId, "Production", "WorkOrder", "Create");
// ============================================================

using System.Security.Claims;
using Calcifer.Api.DbContexts.Rbac.Interfaces;

namespace Calcifer.Api.DbContexts.Rbac.Extensions
{
	public static class RbacExtensions
	{
		// ── ClaimsPrincipal extensions (JWT fast path) ───────────

		/// <summary>
		/// Checks the JWT claims synchronously. O(1).
		/// Use this inside controllers where the principal is available.
		/// </summary>
		public static bool HasPermission(
			this ClaimsPrincipal user,
			string module, string resource, string action)
		{
			if (user?.Identity?.IsAuthenticated != true) return false;

			var claim = $"{module}:{resource}:{action}";
			return user.HasClaim("perms", claim)
				|| user.HasClaim("perms", $"{module}:*:*")
				|| user.HasClaim("perms", "*:*:*");
		}

		/// <summary>
		/// Returns all permission claims from the JWT as a set.
		/// </summary>
		public static IReadOnlySet<string> GetPermissions(this ClaimsPrincipal user)
		{
			return user.FindAll("perms")
					   .Select(c => c.Value)
					   .ToHashSet();
		}


		public static IEnumerable<(string Unit, string Role)> GetUnitRoles(
			this ClaimsPrincipal user)
		{
			return user.FindAll("unit_role")
					   .Select(c =>
					   {
						   var parts = c.Value.Split(':', 2);
						   return parts.Length == 2
							   ? (parts[0], parts[1])
							   : (c.Value, string.Empty);
					   });
		}

		// ── IRbacService extensions (DB-backed async) ────────────

		public static async Task RequireAsync(
			this IRbacService rbac,
			string userId, string module, string resource, string action,
			int? unitId = null, CancellationToken ct = default)
		{
			var hasPermission = await rbac.HasPermissionAsync(
				userId, module, resource, action, unitId, ct);

			if (!hasPermission)
				throw new UnauthorizedAccessException(
					$"User '{userId}' does not have permission '{module}:{resource}:{action}'.");
		}

		public static async Task<IEnumerable<T>> FilterByPermissionAsync<T>(
			this IRbacService rbac,
			IEnumerable<T> items,
			string userId,
			Func<T, Task<bool>> permissionCheck)
		{
			var results = new List<T>();
			foreach (var item in items)
			{
				if (await permissionCheck(item))
					results.Add(item);
			}
			return results;
		}

		// ── "Self" scope helper ──────────────────────────────────

		public static bool IsSelf(this ClaimsPrincipal user, string targetUserId)
		{
			var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
			return !string.IsNullOrEmpty(currentUserId)
				&& currentUserId == targetUserId;
		}
	}
}
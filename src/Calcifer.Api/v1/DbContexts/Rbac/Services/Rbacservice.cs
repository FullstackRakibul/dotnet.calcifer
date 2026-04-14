// ============================================================
//  RbacService.cs
//  Complete RBAC engine implementation.
//
//  Permission resolution algorithm (in order):
//      1. Load all active UserUnitRoles for the user
//      2. Union all RolePermissions across those roles
//      3. Load all active UserDirectPermissions
//      4. Apply denies first, then grants (denies win)
//      5. Store in PermissionCache
//      6. Return as a HashSet<string> of "Module:Resource:Action"
// ============================================================

using System.Text.Json;
using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.Rbac.DTOs;
using Calcifer.Api.DbContexts.Rbac.Entities;
using Calcifer.Api.DbContexts.Rbac.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.DbContexts.Rbac.Services
{
	public class RbacService : IRbacService
	{
		private readonly CalciferAppDbContext _db;
		private readonly ILogger<RbacService> _logger;

		public RbacService(CalciferAppDbContext db, ILogger<RbacService> logger)
		{
			_db = db;
			_logger = logger;
		}

		// ── Core resolution ──────────────────────────────────────

		public async Task<IReadOnlySet<string>> GetPermissionsAsync(
			string userId, int? unitId = null, CancellationToken ct = default)
		{
			// 1. Try cache first
			var cached = await _db.PermissionCache
				.Where(pc => pc.UserId == userId
						  && pc.UnitId == unitId
						  && pc.InvalidatedAt == null)
				.FirstOrDefaultAsync(ct);

			if (cached != null)
			{
				var fromCache = JsonSerializer.Deserialize<HashSet<string>>(cached.PermissionSetJson)
								?? new HashSet<string>();
				return fromCache;
			}

			// 2. Compute fresh
			var permissions = await ComputePermissionsAsync(userId, unitId, ct);

			// 3. Write to cache
			await UpsertCacheAsync(userId, unitId, permissions, ct);

			return permissions;
		}

		public async Task<bool> HasPermissionAsync(
			string userId, string module, string resource, string action,
			int? unitId = null, CancellationToken ct = default)
		{
			var permissions = await GetPermissionsAsync(userId, unitId, ct);
			var claim = $"{module}:{resource}:{action}";

			// SuperAdmin wildcard: if they have Module:*:* they have everything in that module
			return permissions.Contains(claim)
				|| permissions.Contains($"{module}:*:*")
				|| permissions.Contains("*:*:*");
		}

		public async Task<IEnumerable<string>> BuildJwtPermissionClaimsAsync(
			string userId, CancellationToken ct = default)
		{
			// Build the global (cross-unit merged) permission set for JWT
			var permissions = await GetPermissionsAsync(userId, unitId: null, ct);
			return permissions;
		}

		// ── Cache ────────────────────────────────────────────────

		public async Task InvalidateCacheAsync(string userId, CancellationToken ct = default)
		{
			var now = DateTime.UtcNow;
			await _db.PermissionCache
				.Where(pc => pc.UserId == userId && pc.InvalidatedAt == null)
				.ExecuteUpdateAsync(s => s.SetProperty(pc => pc.InvalidatedAt, now), ct);

			_logger.LogInformation("Permission cache invalidated for user {UserId}", userId);
		}

		public async Task RecomputeCacheAsync(string userId, CancellationToken ct = default)
		{
			// Invalidate old entries
			await InvalidateCacheAsync(userId, ct);

			// Get all units this user is assigned to
			var unitIds = await _db.UserUnitRoles
				.Where(r => r.UserId == userId && r.IsActive)
				.Select(r => (int?)r.UnitId)
				.Distinct()
				.ToListAsync(ct);

			unitIds.Add(null); // also compute global

			foreach (var unitId in unitIds)
			{
				var permissions = await ComputePermissionsAsync(userId, unitId, ct);
				await UpsertCacheAsync(userId, unitId, permissions, ct);
			}
		}

		// ── Role management ──────────────────────────────────────

		public async Task AssignUserToUnitRoleAsync(
			string userId, string roleId, int unitId,
			DateTime? validTo = null, string? assignedBy = null,
			CancellationToken ct = default)
		{
			var existing = await _db.UserUnitRoles
				.FirstOrDefaultAsync(r => r.UserId == userId && r.UnitId == unitId && r.RoleId == roleId, ct);

			if (existing != null)
			{
				// Update existing (re-activate or extend validity)
				existing.IsActive = true;
				existing.ValidFrom = DateTime.UtcNow;
				existing.ValidTo = validTo;
				existing.UpdatedAt = DateTime.UtcNow;
				existing.UpdatedBy = assignedBy;
			}
			else
			{
				_db.UserUnitRoles.Add(new UserUnitRole
				{
					UserId = userId,
					RoleId = roleId,
					UnitId = unitId,
					ValidFrom = DateTime.UtcNow,
					ValidTo = validTo,
					IsActive = true,
					CreatedBy = assignedBy
				});
			}

			await _db.SaveChangesAsync(ct);
			await InvalidateCacheAsync(userId, ct);
		}

		public async Task RevokeUserUnitRoleAsync(
			string userId, string roleId, int unitId, CancellationToken ct = default)
		{
			var row = await _db.UserUnitRoles
				.FirstOrDefaultAsync(r => r.UserId == userId && r.RoleId == roleId && r.UnitId == unitId, ct);

			if (row != null)
			{
				row.IsActive = false;
				row.ValidTo = DateTime.UtcNow;
				row.UpdatedAt = DateTime.UtcNow;
				await _db.SaveChangesAsync(ct);
				await InvalidateCacheAsync(userId, ct);
			}
		}

		public async Task<IEnumerable<UserUnitRoleDto>> GetUserUnitRolesAsync(
			string userId, CancellationToken ct = default)
		{
			return await _db.UserUnitRoles
				.Include(r => r.Role)
				.Include(r => r.Unit)
				.Where(r => r.UserId == userId && r.IsActive)
				.Select(r => new UserUnitRoleDto
				{
					RoleId = r.RoleId,
					RoleName = r.Role.Name ?? string.Empty,
					UnitId = r.UnitId,
					UnitName = r.Unit.Name,
					ValidFrom = r.ValidFrom,
					ValidTo = r.ValidTo
				})
				.ToListAsync(ct);
		}

		// ── Direct permission overrides ──────────────────────────

		public async Task SetDirectPermissionAsync(
			string userId, int permissionId, bool isGranted,
			DateTime? expiresAt = null, string? reason = null,
			string? grantedBy = null, CancellationToken ct = default)
		{
			var existing = await _db.UserDirectPermissions
				.FirstOrDefaultAsync(dp => dp.UserId == userId && dp.PermissionId == permissionId, ct);

			if (existing != null)
			{
				existing.IsGranted = isGranted;
				existing.ExpiresAt = expiresAt;
				existing.Reason = reason;
				existing.GrantedBy = grantedBy;
				existing.UpdatedAt = DateTime.UtcNow;
			}
			else
			{
				_db.UserDirectPermissions.Add(new UserDirectPermission
				{
					UserId = userId,
					PermissionId = permissionId,
					IsGranted = isGranted,
					ExpiresAt = expiresAt,
					Reason = reason,
					GrantedBy = grantedBy
				});
			}

			await _db.SaveChangesAsync(ct);
			await InvalidateCacheAsync(userId, ct);
		}

		public async Task RemoveDirectPermissionAsync(
			string userId, int permissionId, CancellationToken ct = default)
		{
			await _db.UserDirectPermissions
				.Where(dp => dp.UserId == userId && dp.PermissionId == permissionId)
				.ExecuteDeleteAsync(ct);

			await InvalidateCacheAsync(userId, ct);
		}

		// ── Role-permission management ───────────────────────────

		public async Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(
			string roleId, CancellationToken ct = default)
		{
			return await _db.RolePermissions
				.Include(rp => rp.Permission)
				.Where(rp => rp.RoleId == roleId)
				.Select(rp => new PermissionDto
				{
					Id = rp.Permission.Id,
					Module = rp.Permission.Module,
					Resource = rp.Permission.Resource,
					Action = rp.Permission.Action,
					Description = rp.Permission.Description
				})
				.ToListAsync(ct);
		}

		public async Task AssignPermissionToRoleAsync(
			string roleId, int permissionId, string? assignedBy = null, CancellationToken ct = default)
		{
			var exists = await _db.RolePermissions
				.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, ct);

			if (!exists)
			{
				_db.RolePermissions.Add(new RolePermission
				{
					RoleId = roleId,
					PermissionId = permissionId,
					AssignedBy = assignedBy
				});
				await _db.SaveChangesAsync(ct);
			}
		}

		public async Task RevokePermissionFromRoleAsync(
			string roleId, int permissionId, CancellationToken ct = default)
		{
			await _db.RolePermissions
				.Where(rp => rp.RoleId == roleId && rp.PermissionId == permissionId)
				.ExecuteDeleteAsync(ct);
		}

		// ── Private helpers ──────────────────────────────────────

		private async Task<HashSet<string>> ComputePermissionsAsync(
			string userId, int? unitId, CancellationToken ct)
		{
			var now = DateTime.UtcNow;

			// Step 1: Get all active unit-role assignments
			var roleQuery = _db.UserUnitRoles
				.Where(r => r.UserId == userId
						 && r.IsActive
						 && r.ValidFrom <= now
						 && (r.ValidTo == null || r.ValidTo > now));

			// Filter by unit if specified; null = global merge
			if (unitId.HasValue)
				roleQuery = roleQuery.Where(r => r.UnitId == unitId.Value);

			var roleIds = await roleQuery.Select(r => r.RoleId).Distinct().ToListAsync(ct);

			// Step 2: Get all permissions for those roles
			var rolePermissions = await _db.RolePermissions
				.Include(rp => rp.Permission)
				.Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission.IsActive)
				.Select(rp => rp.Permission.ClaimValue)
				.ToHashSetAsync(ct);

			// Step 3: Get all active direct overrides
			var directOverrides = await _db.UserDirectPermissions
				.Include(dp => dp.Permission)
				.Where(dp => dp.UserId == userId
						  && (dp.ExpiresAt == null || dp.ExpiresAt > now))
				.ToListAsync(ct);

			// Step 4: Apply overrides — denies first, then grants
			var denied = directOverrides
				.Where(d => !d.IsGranted)
				.Select(d => d.Permission.ClaimValue)
				.ToHashSet();

			var granted = directOverrides
				.Where(d => d.IsGranted)
				.Select(d => d.Permission.ClaimValue)
				.ToHashSet();

			// Remove denied
			rolePermissions.ExceptWith(denied);
			// Add direct grants
			rolePermissions.UnionWith(granted);

			return rolePermissions;
		}

		private async Task UpsertCacheAsync(
			string userId, int? unitId, HashSet<string> permissions, CancellationToken ct)
		{
			var json = JsonSerializer.Serialize(permissions);
			var existing = await _db.PermissionCache
				.FirstOrDefaultAsync(pc => pc.UserId == userId && pc.UnitId == unitId, ct);

			if (existing != null)
			{
				existing.PermissionSetJson = json;
				existing.ComputedAt = DateTime.UtcNow;
				existing.InvalidatedAt = null;
			}
			else
			{
				_db.PermissionCache.Add(new PermissionCache
				{
					UserId = userId,
					UnitId = unitId,
					PermissionSetJson = json
				});
			}

			await _db.SaveChangesAsync(ct);
		}
	}
}
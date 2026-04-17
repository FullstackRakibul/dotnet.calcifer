using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DTOs.RbacDTO;
using Calcifer.Api.DbContexts.Rbac.Entities;
using Calcifer.Api.Interface.Rbac;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Calcifer.Api.Services.Rbac
{
	public class RbacService : IRbacService
	{
		private readonly CalciferAppDbContext _db;
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly RoleManager<ApplicationRole> _roleMgr;
		private readonly ILogger<RbacService> _log;

		// Sentinel UnitId used for the global PermissionCache row
		// null = global (cross-unit) — matches the entity's nullable FK to OrganizationUnits
		private static readonly int? GlobalUnitId = null;

		public RbacService(
			CalciferAppDbContext db,
			UserManager<ApplicationUser> userMgr,
			RoleManager<ApplicationRole> roleMgr,
			ILogger<RbacService> log)
		{
			_db = db;
			_userMgr = userMgr;
			_roleMgr = roleMgr;
			_log = log;
		}

		// ────────────────────────────────────────────────────────────────────
		//  RESOLUTION
		// ────────────────────────────────────────────────────────────────────

		public async Task<IReadOnlySet<string>> GetPermissionsAsync(
			string userId, CancellationToken ct = default)
		{
			var cache = await _db.PermissionCache
				.AsNoTracking()
				.FirstOrDefaultAsync(pc => pc.UserId == userId && pc.UnitId == GlobalUnitId, ct);

			// Stale = older than 5 minutes or missing
			var isStale = cache == null
					   || cache.GeneratedAt < DateTime.UtcNow.AddMinutes(-5);

			if (isStale)
			{
				await RecomputeCacheAsync(userId, ct);
				cache = await _db.PermissionCache
					.AsNoTracking()
					.FirstOrDefaultAsync(pc => pc.UserId == userId && pc.UnitId == GlobalUnitId, ct);
			}

			if (cache == null) return new HashSet<string>();

			return JsonSerializer.Deserialize<HashSet<string>>(cache.PermissionsJson)
				   ?? new HashSet<string>();
		}

		public async Task<bool> HasPermissionAsync(
			string userId, string module, string resource, string action,
			CancellationToken ct = default)
		{
			var perms = await GetPermissionsAsync(userId, ct);
			var key = $"{module}:{resource}:{action}";

			// wildcard support: Module:*:* and *:*:*
			return perms.Contains(key)
				|| perms.Contains($"{module}:*:*")
				|| perms.Contains("*:*:*");
		}

		public async Task<IEnumerable<string>> BuildJwtPermissionClaimsAsync(
			string userId, CancellationToken ct = default)
		{
			await RecomputeCacheAsync(userId, ct);
			return await GetPermissionsAsync(userId, ct);
		}

		// ────────────────────────────────────────────────────────────────────
		//  CACHE
		// ────────────────────────────────────────────────────────────────────

		public async Task InvalidateCacheAsync(string userId, CancellationToken ct = default)
		{
			var rows = await _db.PermissionCache
				.Where(pc => pc.UserId == userId)
				.ToListAsync(ct);

			_db.PermissionCache.RemoveRange(rows);
			await _db.SaveChangesAsync(ct);

			_log.LogInformation("Permission cache invalidated for user {UserId}", userId);
		}

		public async Task RecomputeCacheAsync(string userId, CancellationToken ct = default)
		{
			_log.LogInformation("Recomputing permission cache for user {UserId}", userId);
			var now = DateTime.UtcNow;

			// ── Step 1: active role IDs from UserUnitRoles ───────────────
			var roleIds = await _db.UserUnitRoles
				.Where(uur => uur.UserId == userId
						   && !uur.IsDeleted
						   && (uur.ValidTo == null || uur.ValidTo > now))
				.Select(uur => uur.RoleId)
				.Distinct()
				.ToListAsync(ct);

			// ── Step 2: all permissions granted by those roles ───────────
			var roleKeys = await _db.RolePermissions
				.Where(rp => roleIds.Contains(rp.RoleId) && !rp.IsDeleted)
				.Include(rp => rp.Permission)
				.Select(rp => $"{rp.Permission.Module}:{rp.Permission.Resource}:{rp.Permission.Action}")
				.ToListAsync(ct);

			var permSet = new HashSet<string>(roleKeys);

			// ── Step 3: apply direct overrides ───────────────────────────
			var overrides = await _db.UserDirectPermissions
				.Where(udp => udp.UserId == userId
						   && !udp.IsDeleted
						   && (udp.ExpiresAt == null || udp.ExpiresAt > now))
				.Include(udp => udp.Permission)
				.ToListAsync(ct);

			foreach (var o in overrides)
			{
				var key = $"{o.Permission.Module}:{o.Permission.Resource}:{o.Permission.Action}";
				if (o.IsGranted) permSet.Add(key);
				else permSet.Remove(key);
			}

			// ── Step 4: upsert global cache row ──────────────────────────
			var json = JsonSerializer.Serialize(permSet.OrderBy(k => k).ToList());

			var existing = await _db.PermissionCache
				.IgnoreQueryFilters()
				.FirstOrDefaultAsync(pc => pc.UserId == userId && pc.UnitId == GlobalUnitId, ct);

			if (existing == null)
			{
				_db.PermissionCache.Add(new PermissionCache
				{
					UserId = userId,
					UnitId = GlobalUnitId,
					PermissionsJson = json,
					GeneratedAt = now
				});
			}
			else
			{
				existing.PermissionsJson = json;
				existing.GeneratedAt = now;
			}

			await _db.SaveChangesAsync(ct);
			_log.LogInformation("Cache rebuilt for user {UserId} — {Count} permissions",
				userId, permSet.Count);
		}

		// ────────────────────────────────────────────────────────────────────
		//  USER ↔ ROLE ↔ UNIT
		// ────────────────────────────────────────────────────────────────────

		public async Task<(bool Success, string Message)> AssignUserToUnitRoleAsync(
			string userId, string roleId, int unitId,
			DateTime? validFrom = null, DateTime? validTo = null,
			string? assignedBy = null, CancellationToken ct = default)
		{
			var exists = await _db.UserUnitRoles
				.AnyAsync(uur => uur.UserId == userId
							  && uur.RoleId == roleId
							  && uur.UnitId == unitId
							  && !uur.IsDeleted, ct);

			if (exists) return (false, "User already has this role at this unit.");

			_db.UserUnitRoles.Add(new UserUnitRole
			{
				UserId = userId,
				RoleId = roleId,
				UnitId = unitId,
				ValidFrom = validFrom,
				ValidTo = validTo,
				CreatedBy = assignedBy,
				CreatedAt = DateTime.UtcNow,
				StatusId = 1
			});

			// Keep ASP.NET Identity UserRoles in sync so JWT ClaimTypes.Role works
			var user = await _userMgr.FindByIdAsync(userId);
			var role = await _roleMgr.FindByIdAsync(roleId);
			if (user != null && role?.Name != null
				&& !await _userMgr.IsInRoleAsync(user, role.Name))
			{
				await _userMgr.AddToRoleAsync(user, role.Name);
			}

			await _db.SaveChangesAsync(ct);
			await RecomputeCacheAsync(userId, ct);

			return (true, "Role assigned successfully.");
		}

		public async Task<(bool Success, string Message)> RevokeUserUnitRoleAsync(
			string userId, string roleId, int unitId,
			string? revokedBy = null, CancellationToken ct = default)
		{
			var row = await _db.UserUnitRoles
				.FirstOrDefaultAsync(uur => uur.UserId == userId
										 && uur.RoleId == roleId
										 && uur.UnitId == unitId
										 && !uur.IsDeleted, ct);

			if (row == null) return (false, "Role assignment not found.");

			row.IsDeleted = true;
			row.DeletedAt = DateTime.UtcNow;
			row.DeletedBy = revokedBy;

			// Remove from Identity only if no other unit still holds this role
			var stillHolds = await _db.UserUnitRoles
				.AnyAsync(uur => uur.UserId == userId
							  && uur.RoleId == roleId
							  && uur.UnitId != unitId
							  && !uur.IsDeleted, ct);

			if (!stillHolds)
			{
				var user = await _userMgr.FindByIdAsync(userId);
				var role = await _roleMgr.FindByIdAsync(roleId);
				if (user != null && role?.Name != null)
					await _userMgr.RemoveFromRoleAsync(user, role.Name);
			}

			await _db.SaveChangesAsync(ct);
			await RecomputeCacheAsync(userId, ct);

			return (true, "Role revoked successfully.");
		}

		public async Task<IEnumerable<UserUnitRoleDto>> GetUserUnitRolesAsync(
			string userId, CancellationToken ct = default)
		{
			var now = DateTime.UtcNow;

			return await _db.UserUnitRoles
				.Where(uur => uur.UserId == userId && !uur.IsDeleted)
				.Include(uur => uur.Role)
				.Include(uur => uur.Unit)
				.Select(uur => new UserUnitRoleDto(
					uur.UserId,
					uur.RoleId,
					uur.Role.Name ?? string.Empty,
					uur.UnitId,
					uur.Unit.Name,
					uur.ValidFrom,
					uur.ValidTo,
					uur.ValidTo == null || uur.ValidTo > now
				))
				.ToListAsync(ct);
		}

		// ────────────────────────────────────────────────────────────────────
		//  DIRECT PERMISSION OVERRIDES
		// ────────────────────────────────────────────────────────────────────

		public async Task<(bool Success, string Message)> SetDirectPermissionAsync(
			string userId, int permissionId, bool isGranted,
			DateTime? expiresAt = null, string? grantedBy = null,
			CancellationToken ct = default)
		{
			var existing = await _db.UserDirectPermissions
				.IgnoreQueryFilters()
				.FirstOrDefaultAsync(udp => udp.UserId == userId
										 && udp.PermissionId == permissionId, ct);

			var now = DateTime.UtcNow;

			if (existing == null)
			{
				_db.UserDirectPermissions.Add(new UserDirectPermission
				{
					UserId = userId,
					PermissionId = permissionId,
					IsGranted = isGranted,
					ExpiresAt = expiresAt,
					GrantedBy = grantedBy,
					CreatedAt = now,
					CreatedBy = grantedBy,
					IsDeleted = false,
					StatusId = 1
				});
			}
			else
			{
				existing.IsGranted = isGranted;
				existing.ExpiresAt = expiresAt;
				existing.GrantedBy = grantedBy;
				existing.IsDeleted = false;
				existing.UpdatedAt = now;
				existing.UpdatedBy = grantedBy;
			}

			await _db.SaveChangesAsync(ct);
			await RecomputeCacheAsync(userId, ct);

			var verb = isGranted ? "granted" : "denied";
			return (true, $"Permission {verb} successfully.");
		}

		public async Task<(bool Success, string Message)> RemoveDirectPermissionAsync(
			string userId, int permissionId,
			string? removedBy = null, CancellationToken ct = default)
		{
			var row = await _db.UserDirectPermissions
				.FirstOrDefaultAsync(udp => udp.UserId == userId
										 && udp.PermissionId == permissionId
										 && !udp.IsDeleted, ct);

			if (row == null) return (false, "Direct permission override not found.");

			row.IsDeleted = true;
			row.DeletedAt = DateTime.UtcNow;
			row.DeletedBy = removedBy;

			await _db.SaveChangesAsync(ct);
			await RecomputeCacheAsync(userId, ct);

			return (true, "Direct permission override removed.");
		}

		// ────────────────────────────────────────────────────────────────────
		//  ROLE ↔ PERMISSION CONFIG
		// ────────────────────────────────────────────────────────────────────

		public async Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(
			string roleId, CancellationToken ct = default)
		{
			return await _db.RolePermissions
				.Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
				.Include(rp => rp.Permission)
				.Select(rp => new PermissionDto(
					rp.Permission.Id,
					rp.Permission.Module,
					rp.Permission.Resource,
					rp.Permission.Action,
					rp.Permission.Description
				))
				.ToListAsync(ct);
		}

		public async Task<(bool Success, string Message)> AssignPermissionToRoleAsync(
			string roleId, int permissionId,
			string? assignedBy = null, CancellationToken ct = default)
		{
			var exists = await _db.RolePermissions
				.AnyAsync(rp => rp.RoleId == roleId
							 && rp.PermissionId == permissionId
							 && !rp.IsDeleted, ct);

			if (exists) return (false, "Role already has this permission.");

			_db.RolePermissions.Add(new RolePermission
			{
				RoleId = roleId,
				PermissionId = permissionId,
				CreatedBy = assignedBy,
				CreatedAt = DateTime.UtcNow,
				StatusId = 1
			});

			await _db.SaveChangesAsync(ct);
			await _RebuildForRoleAsync(roleId, ct);

			return (true, "Permission assigned to role.");
		}

		public async Task<(bool Success, string Message)> RevokePermissionFromRoleAsync(
			string roleId, int permissionId,
			string? revokedBy = null, CancellationToken ct = default)
		{
			var row = await _db.RolePermissions
				.FirstOrDefaultAsync(rp => rp.RoleId == roleId
										&& rp.PermissionId == permissionId
										&& !rp.IsDeleted, ct);

			if (row == null) return (false, "Role does not have this permission.");

			row.IsDeleted = true;
			row.DeletedAt = DateTime.UtcNow;
			row.DeletedBy = revokedBy;

			await _db.SaveChangesAsync(ct);
			await _RebuildForRoleAsync(roleId, ct);

			return (true, "Permission revoked from role.");
		}

		// ────────────────────────────────────────────────────────────────────
		//  PRIVATE HELPERS
		// ────────────────────────────────────────────────────────────────────

		/// <summary>Rebuild cache for every user who holds a given role.</summary>
		private async Task _RebuildForRoleAsync(string roleId, CancellationToken ct)
		{
			var userIds = await _db.UserUnitRoles
				.Where(uur => uur.RoleId == roleId && !uur.IsDeleted)
				.Select(uur => uur.UserId)
				.Distinct()
				.ToListAsync(ct);

			foreach (var uid in userIds)
				await RecomputeCacheAsync(uid, ct);
		}
	}
}
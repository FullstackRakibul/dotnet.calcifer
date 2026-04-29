using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.MinimalApis
{
	public static class RbacMinimalApi
	{
		public static void RegisterRbacApis(this IEndpointRouteBuilder app)
		{
			var group = app
				.MapGroup("/rbac")
				.WithTags("RBAC Management")
				.RequireAuthorization("SuperAdminPolicy");

			// ── PERMISSIONS (read-only catalogue) ────────────────────────

			// GET /rbac/permissions
			group.MapGet("/permissions", async (CalciferAppDbContext db) =>
			{
				var list = await db.Permissions
					.Where(p => p.IsActive)
					.OrderBy(p => p.Module)
					.ThenBy(p => p.Resource)
					.ThenBy(p => p.Action)
					.Select(p => new PermissionDto(p.Id, p.Module, p.Resource, p.Action, p.Description))
					.ToListAsync();

				return Results.Ok(new { status = true, data = list, count = list.Count });
			})
			.WithSummary("List all active permissions in the catalogue");

			// ── ROLE PERMISSIONS ──────────────────────────────────────────

			// GET /rbac/roles/{roleId}/permissions
			group.MapGet("/roles/{roleId}/permissions", async (
				string roleId,
				IRoleManagementService rbac) =>
			{
				var perms = await rbac.GetRolePermissionsAsync(roleId);
				return Results.Ok(new { status = true, data = perms });
			})
			.WithSummary("Get all permissions assigned to a role");

			// POST /rbac/roles/{roleId}/permissions
			group.MapPost("/roles/{roleId}/permissions", async (
				string roleId,
				AssignRolePermissionRequest req,
				IRoleManagementService rbac,
				HttpContext ctx) =>
			{
				var actorId = ctx.User.FindFirst("ID")?.Value ?? "system";
				var (ok, msg) = await rbac.AssignPermissionToRoleAsync(roleId, req.PermissionId, actorId);

				return ok
					? Results.Ok(new { status = true, message = msg })
					: Results.Conflict(new { status = false, message = msg });
			})
			.WithSummary("Assign a permission to a role");

			// DELETE /rbac/roles/{roleId}/permissions/{permId}
			group.MapDelete("/roles/{roleId}/permissions/{permId:int}", async (
				string roleId,
				int permId,
				IRoleManagementService rbac,
				HttpContext ctx) =>
			{
				var actorId = ctx.User.FindFirst("ID")?.Value ?? "system";
				var (ok, msg) = await rbac.RevokePermissionFromRoleAsync(roleId, permId, actorId);

				return ok
					? Results.Ok(new { status = true, message = msg })
					: Results.NotFound(new { status = false, message = msg });
			})
			.WithSummary("Revoke a permission from a role");

			// ── USER UNIT ROLES ───────────────────────────────────────────

			// GET /rbac/users/{userId}/roles
			group.MapGet("/users/{userId}/roles", async (
				string userId,
				IRoleManagementService rbac) =>
			{
				var roles = await rbac.GetUserUnitRolesAsync(userId);
				return Results.Ok(new { status = true, data = roles });
			})
			.WithSummary("List a user's unit-role assignments");

			// POST /rbac/users/{userId}/roles
			group.MapPost("/users/{userId}/roles", async (
				string userId,
				AssignUnitRoleRequest req,
				IRoleManagementService rbac,
				HttpContext ctx) =>
			{
				var actorId = ctx.User.FindFirst("ID")?.Value ?? "system";
				var (ok, msg) = await rbac.AssignUserToUnitRoleAsync(
					userId, req.RoleId, req.UnitId,
					req.ValidFrom, req.ValidTo, actorId);

				return ok
					? Results.Ok(new { status = true, message = msg })
					: Results.Conflict(new { status = false, message = msg });
			})
			.WithSummary("Assign a user to a role at an org unit");

			// DELETE /rbac/users/{userId}/roles?roleId=xxx&unitId=yyy
			group.MapDelete("/users/{userId}/roles", async (
				string userId,
				string roleId,
				int unitId,
				IRoleManagementService rbac,
				HttpContext ctx) =>
			{
				var actorId = ctx.User.FindFirst("ID")?.Value ?? "system";
				var (ok, msg) = await rbac.RevokeUserUnitRoleAsync(
					userId, roleId, unitId, actorId);

				return ok
					? Results.Ok(new { status = true, message = msg })
					: Results.NotFound(new { status = false, message = msg });
			})
			.WithSummary("Revoke a user's role at an org unit");

			// ── EFFECTIVE PERMISSION SUMMARY ──────────────────────────────

			// GET /rbac/users/{userId}/permissions
			group.MapGet("/users/{userId}/permissions", async (
				string userId,
				IRoleManagementService rbac,
				UserManager<ApplicationUser> userMgr,
				CalciferAppDbContext db) =>
			{
				var user = await userMgr.FindByIdAsync(userId);
				if (user == null)
					return Results.NotFound(new { status = false, message = "User not found." });

				var perms = await rbac.GetPermissionsAsync(userId);
				var roles = await userMgr.GetRolesAsync(user);
				//var cache = await db.PermissionCache.AsNoTracking()
				//				.FirstOrDefaultAsync(pc => pc.UserId == userId && pc.UnitId == 0);
				var cache = await db.PermissionCache.AsNoTracking()
								.FirstOrDefaultAsync(pc => pc.UserId == userId && pc.UnitId == null);

				var directOverrides = await db.UserDirectPermissions
					.Where(udp => udp.UserId == userId && !udp.IsDeleted)
					.Include(udp => udp.Permission)
					.Select(udp => new DirectPermissionDto(
						udp.UserId,
						udp.PermissionId,
						udp.Permission.Module,
						udp.Permission.Resource,
						udp.Permission.Action,
						udp.IsGranted,
						udp.GrantedBy,
						udp.ExpiresAt))
					.ToListAsync();

				var summary = new UserPermissionSummary(
					userId,
					user.Name,
					user.Email ?? string.Empty,
					roles.ToList(),
					perms.OrderBy(k => k).ToList(),
					directOverrides,
					cache?.GeneratedAt ?? DateTime.MinValue,
					cache == null || cache.GeneratedAt < DateTime.UtcNow.AddMinutes(-5)
				);

				return Results.Ok(new { status = true, data = summary });
			})
			.WithSummary("Get a user's full effective permission summary");

			// ── DIRECT PERMISSION OVERRIDES ───────────────────────────────

			// POST /rbac/users/{userId}/direct-permissions
			group.MapPost("/users/{userId}/direct-permissions", async (
				string userId,
				SetDirectPermissionRequest req,
				IRoleManagementService rbac,
				HttpContext ctx) =>
			{
				var actorId = ctx.User.FindFirst("ID")?.Value ?? "system";
				var (ok, msg) = await rbac.SetDirectPermissionAsync(
					userId, req.PermissionId, req.IsGranted,
					req.ExpiresAt, actorId);

				return ok
					? Results.Ok(new { status = true, message = msg })
					: Results.BadRequest(new { status = false, message = msg });
			})
			.WithSummary("Grant or deny a permission directly to a user");

			// DELETE /rbac/users/{userId}/direct-permissions/{permId}
			group.MapDelete("/users/{userId}/direct-permissions/{permId:int}", async (
				string userId,
				int permId,
				IRoleManagementService rbac,
				HttpContext ctx) =>
			{
				var actorId = ctx.User.FindFirst("ID")?.Value ?? "system";
				var (ok, msg) = await rbac.RemoveDirectPermissionAsync(userId, permId, actorId);

				return ok
					? Results.Ok(new { status = true, message = msg })
					: Results.NotFound(new { status = false, message = msg });
			})
			.WithSummary("Remove a direct permission override from a user");

			// ── CACHE MANAGEMENT ──────────────────────────────────────────

			// POST /rbac/users/{userId}/cache/invalidate
			group.MapPost("/users/{userId}/cache/invalidate", async (
				string userId,
				IRoleManagementService rbac) =>
			{
				await rbac.InvalidateCacheAsync(userId);
				return Results.Ok(new
				{
					status = true,
					message = $"Permission cache invalidated for user {userId}."
				});
			})
			.WithSummary("Wipe the permission cache for a user");

			// POST /rbac/users/{userId}/cache/recompute
			group.MapPost("/users/{userId}/cache/recompute", async (
				string userId,
				IRoleManagementService rbac) =>
			{
				await rbac.RecomputeCacheAsync(userId);
				return Results.Ok(new
				{
					status = true,
					message = $"Permission cache recomputed for user {userId}."
				});
			})
			.WithSummary("Force-rebuild the permission cache for a user");
		}
	}
}
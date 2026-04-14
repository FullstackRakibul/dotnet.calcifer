// ============================================================
//  RbacMinimalApi.cs
//  All RBAC management endpoints as Minimal APIs.
//  Isolated: no other module's routes are registered here.
//
//  Endpoints:
//      GET    /rbac/permissions                    — list all permissions
//      GET    /rbac/roles/{roleId}/permissions     — role's permission set
//      POST   /rbac/roles/{roleId}/permissions     — assign permission to role
//      DELETE /rbac/roles/{roleId}/permissions/{permId}
//
//      GET    /rbac/users/{userId}/roles           — user's unit-role assignments
//      POST   /rbac/users/{userId}/roles           — assign user to unit+role
//      DELETE /rbac/users/{userId}/roles           — revoke unit+role
//
//      GET    /rbac/users/{userId}/permissions     — effective permission summary
//      POST   /rbac/users/{userId}/direct-permissions
//      DELETE /rbac/users/{userId}/direct-permissions/{permId}
//
//      POST   /rbac/users/{userId}/cache/invalidate
//      POST   /rbac/users/{userId}/cache/recompute
// ============================================================

using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.Rbac.DTOs;
using Calcifer.Api.DbContexts.Rbac.Interfaces;
using Calcifer.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.AuthHandler.MinimalApis
{
	public static class RbacMinimalApi
	{
		public static IEndpointRouteBuilder MapRbacApis(this IEndpointRouteBuilder app)
		{
			var group = app.MapGroup("/rbac")
						   .WithTags("RBAC")
						   .RequireAuthorization("SuperAdminPolicy");

			// ── Permissions catalogue ────────────────────────────

			group.MapGet("/permissions", async (CalciferAppDbContext db) =>
			{
				var perms = await db.Permissions
					.Where(p => p.IsActive)
					.OrderBy(p => p.Module).ThenBy(p => p.Resource).ThenBy(p => p.Action)
					.Select(p => new PermissionDto
					{
						Id = p.Id,
						Module = p.Module,
						Resource = p.Resource,
						Action = p.Action,
						Description = p.Description
					})
					.ToListAsync();

				return Results.Ok(new ApiResponseDto<IEnumerable<PermissionDto>>
				{
					Status = true,
					Message = $"{perms.Count} permissions",
					Data = perms
				});
			});

			// ── Role permission management ───────────────────────

			group.MapGet("/roles/{roleId}/permissions", async (string roleId, IRbacService rbac) =>
			{
				var perms = await rbac.GetRolePermissionsAsync(roleId);
				return Results.Ok(new ApiResponseDto<IEnumerable<PermissionDto>>
				{
					Status = true,
					Data = perms
				});
			});

			group.MapPost("/roles/{roleId}/permissions", async (
				string roleId, AssignRolePermissionRequest req,
				IRbacService rbac, HttpContext ctx) =>
			{
				var caller = ctx.User.Identity?.Name;
				await rbac.AssignPermissionToRoleAsync(roleId, req.PermissionId, caller);
				return Results.Ok(new ApiResponseDto<string>
				{
					Status = true,
					Message = "Permission assigned to role."
				});
			});

			group.MapDelete("/roles/{roleId}/permissions/{permId:int}", async (
				string roleId, int permId, IRbacService rbac) =>
			{
				await rbac.RevokePermissionFromRoleAsync(roleId, permId);
				return Results.NoContent();
			});

			// ── User unit-role management ─────────────────────────

			group.MapGet("/users/{userId}/roles", async (string userId, IRbacService rbac) =>
			{
				var roles = await rbac.GetUserUnitRolesAsync(userId);
				return Results.Ok(new ApiResponseDto<IEnumerable<UserUnitRoleDto>>
				{
					Status = true,
					Data = roles
				});
			});

			group.MapPost("/users/{userId}/roles", async (
				string userId, AssignUnitRoleRequest req,
				IRbacService rbac, HttpContext ctx) =>
			{
				var caller = ctx.User.Identity?.Name;
				await rbac.AssignUserToUnitRoleAsync(userId, req.RoleId, req.UnitId, req.ValidTo, caller);
				return Results.Ok(new ApiResponseDto<string>
				{
					Status = true,
					Message = "User assigned to unit role."
				});
			});

			group.MapDelete("/users/{userId}/roles", async (
				string userId, string roleId, int unitId, IRbacService rbac) =>
			{
				await rbac.RevokeUserUnitRoleAsync(userId, roleId, unitId);
				return Results.NoContent();
			});

			// ── User permission summary ──────────────────────────

			group.MapGet("/users/{userId}/permissions", async (
				string userId, IRbacService rbac, CalciferAppDbContext db) =>
			{
				var user = await db.Users.FindAsync(userId);
				if (user == null) return Results.NotFound("User not found.");

				var unitRoles = await rbac.GetUserUnitRolesAsync(userId);
				var effectivePerms = await rbac.GetPermissionsAsync(userId);

				var directOverrides = await db.UserDirectPermissions
					.Include(dp => dp.Permission)
					.Where(dp => dp.UserId == userId)
					.Select(dp => new DirectOverrideDto
					{
						PermissionId = dp.PermissionId,
						ClaimValue = $"{dp.Permission.Module}:{dp.Permission.Resource}:{dp.Permission.Action}",
						IsGranted = dp.IsGranted,
						ExpiresAt = dp.ExpiresAt,
						Reason = dp.Reason,
						GrantedBy = dp.GrantedBy
					})
					.ToListAsync();

				var summary = new UserPermissionSummaryDto
				{
					UserId = userId,
					UserName = user.Name,
					UnitRoles = unitRoles,
					EffectivePermissions = effectivePerms,
					DirectOverrides = directOverrides
				};

				return Results.Ok(new ApiResponseDto<UserPermissionSummaryDto>
				{
					Status = true,
					Data = summary
				});
			});

			// ── Direct permission overrides ──────────────────────

			group.MapPost("/users/{userId}/direct-permissions", async (
				string userId, SetDirectPermissionRequest req,
				IRbacService rbac, HttpContext ctx) =>
			{
				var caller = ctx.User.Identity?.Name;
				await rbac.SetDirectPermissionAsync(
					userId, req.PermissionId, req.IsGranted,
					req.ExpiresAt, req.Reason, caller);

				return Results.Ok(new ApiResponseDto<string>
				{
					Status = true,
					Message = req.IsGranted
						? "Permission granted directly to user."
						: "Permission explicitly denied for user."
				});
			});

			group.MapDelete("/users/{userId}/direct-permissions/{permId:int}", async (
				string userId, int permId, IRbacService rbac) =>
			{
				await rbac.RemoveDirectPermissionAsync(userId, permId);
				return Results.NoContent();
			});

			// ── Cache management ─────────────────────────────────

			group.MapPost("/users/{userId}/cache/invalidate", async (
				string userId, IRbacService rbac) =>
			{
				await rbac.InvalidateCacheAsync(userId);
				return Results.Ok(new ApiResponseDto<string>
				{
					Status = true,
					Message = "Cache invalidated."
				});
			});

			group.MapPost("/users/{userId}/cache/recompute", async (
				string userId, IRbacService rbac) =>
			{
				await rbac.RecomputeCacheAsync(userId);
				return Results.Ok(new ApiResponseDto<string>
				{
					Status = true,
					Message = "Cache recomputed."
				});
			});

			return app;
		}
	}
}
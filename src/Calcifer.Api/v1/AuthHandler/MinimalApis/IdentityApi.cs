// ============================================================
//  IdentityApi.cs
//  Registers the /auth/** and /roles/** Minimal API groups.
//  This is the implementation of app.MapIdentityApis()
//  called in MiddlewareDependencyInversion.cs.
//
//  Route map:
//
//  Auth (public — no token needed):
//      POST   /auth/register              — create a new user
//      POST   /auth/login                 — get a JWT
//
//  Auth (requires own token):
//      GET    /auth/me                    — own profile
//      POST   /auth/change-password       — change own password
//
//  Role management (SuperAdmin only):
//      GET    /roles                      — list all roles
//      POST   /roles                      — create a custom role
//      DELETE /roles/{roleId}             — delete a custom role
//      POST   /roles/assign               — assign role to user
//      POST   /roles/remove               — remove role from user
//      GET    /roles/user/{userId}        — list user's roles
// ============================================================

using System.Security.Claims;
using Calcifer.Api.DTOs;
using Calcifer.Api.DTOs.AuthDTO;
using Calcifer.Api.Services.AuthService;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcifer.Api.AuthHandler.MinimalApis
{
    public static class IdentityApi
    {
        public static IEndpointRouteBuilder MapIdentityApis(this IEndpointRouteBuilder app)
        {
            // ── Auth endpoints ────────────────────────────────────────

            var auth = app.MapGroup("/auth")
                          .WithTags("Auth");

            // POST /auth/register — open, no auth required
            auth.MapPost("/register", async (
                RegisterRequestDto dto,
                AuthService authService,
                HttpContext ctx) =>
            {
                // Detect caller role to gate privileged role assignment
                var callerRole = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
                var (success, message, profile) = await authService.RegisterAsync(dto, callerRole);

                return success
                    ? Results.Created($"/auth/me",
                        new ApiResponseDto<UserProfileDto> { Status = true, Message = message, Data = profile })
                    : Results.BadRequest(
                        new ApiResponseDto<string> { Status = false, Message = message });
            });

            // POST /auth/login — open, no auth required
            auth.MapPost("/login", async (LoginRequestDto dto, AuthService authService) =>
            {
                //return Results.Ok("this is a testttttttttttt.............s");
                var (success, message, response) = await authService.LoginAsync(dto);

                return success
                    ? Results.Ok(new ApiResponseDto<LoginResponseDto> { Status = true, Message = message, Data = response })
                    : Results.Unauthorized();
            });

            // GET /auth/me — requires own token
            auth.MapGet("/me", async (HttpContext ctx, AuthService authService) =>
            {
                var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var profile = await authService.GetProfileAsync(userId);
                return profile == null
                    ? Results.NotFound(new ApiResponseDto<string> { Status = false, Message = "User not found." })
                    : Results.Ok(new ApiResponseDto<UserProfileDto> { Status = true, Data = profile });
            })
            .RequireAuthorization();

            // POST /auth/change-password — requires own token
            auth.MapPost("/change-password", async (
                ChangePasswordDto dto,
                HttpContext ctx,
                AuthService authService) =>
            {
                var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var (success, message) = await authService.ChangePasswordAsync(userId, dto);
                return Results.Ok(new ApiResponseDto<string> { Status = success, Message = message });
            })
            .RequireAuthorization();

            // ── Role management endpoints (SuperAdmin only) ───────────

            var roles = app.MapGroup("/roles")
                           .WithTags("Roles")
                           .RequireAuthorization("SuperAdminPolicy");

            // GET /roles
            roles.MapGet("/", async (RoleService roleService) =>
            {
                var all = await roleService.GetAllRolesAsync();
                return Results.Ok(new ApiResponseDto<IEnumerable<RoleResponseDto>>
                {
                    Status = true,
                    Data = all
                });
            });

            // POST /roles
            roles.MapPost("/", async (CreateRoleRequestDto dto, RoleService roleService) =>
            {
                var (success, message, role) = await roleService.CreateRoleAsync(dto);
                return success
                    ? Results.Created($"/roles/{role!.Id}",
                        new ApiResponseDto<RoleResponseDto> { Status = true, Message = message, Data = role })
                    : Results.BadRequest(
                        new ApiResponseDto<string> { Status = false, Message = message });
            });

            // DELETE /roles/{roleId}
            roles.MapDelete("/{roleId}", async (string roleId, RoleService roleService) =>
            {
                try
                {
                    var success = await roleService.DeleteRoleAsync(roleId);
                    return success
                        ? Results.NoContent()
                        : Results.NotFound(new ApiResponseDto<string> { Status = false, Message = "Role not found." });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new ApiResponseDto<string> { Status = false, Message = ex.Message });
                }
            });

            // POST /roles/assign
            roles.MapPost("/assign", async (AssignRoleRequestDto dto, RoleService roleService) =>
            {
                var (success, message) = await roleService.AssignRoleAsync(dto);
                return Results.Ok(new ApiResponseDto<string> { Status = success, Message = message });
            });

            // POST /roles/remove
            roles.MapPost("/remove", async (AssignRoleRequestDto dto, RoleService roleService) =>
            {
                var (success, message) = await roleService.RemoveRoleAsync(dto);
                return Results.Ok(new ApiResponseDto<string> { Status = success, Message = message });
            });

            // GET /roles/user/{userId}
            roles.MapGet("/user/{userId}", async (string userId, RoleService roleService) =>
            {
                var userRoles = await roleService.GetUserRolesAsync(userId);
                return Results.Ok(new ApiResponseDto<IEnumerable<string>>
                {
                    Status = true,
                    Data = userRoles
                });
            });

            return app;
        }
    }
}
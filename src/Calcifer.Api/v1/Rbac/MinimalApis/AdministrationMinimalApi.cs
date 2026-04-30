using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Interfaces;

namespace Calcifer.Api.Rbac.MinimalApis
{
  public static class AdministrationMinimalApi
  {
    public static RouteGroupBuilder RegisterAdministrationApis(this RouteGroupBuilder group, ILogWriter logger)
    {
      // ── OVERVIEW ─────────────────────────────────────────────
      group.MapGet("/overview/stats", GetOverviewStats).WithName("GetAdminOverviewStats").WithSummary("Get admin dashboard overview statistics");

      // ── PERMISSIONS ──────────────────────────────────────────
      group.MapGet("/permissions", GetPermissions).WithName("GetPermissions").WithSummary("Get all available permissions");
      group.MapGet("/permissions/{id}", GetPermissionById).WithName("GetPermissionById").WithSummary("Get specific permission by ID");

      // ── ROLES CRUD ───────────────────────────────────────────
      group.MapGet("/roles", GetAllRoles).WithName("GetAllRoles").WithSummary("Get all roles");
      group.MapGet("/roles/{id}", GetRoleById).WithName("GetRoleById").WithSummary("Get specific role by ID");
      group.MapPost("/roles", CreateRole).WithName("CreateRole").WithSummary("Create new role");
      group.MapPut("/roles/{id}", UpdateRole).WithName("UpdateRole").WithSummary("Update existing role");
      group.MapDelete("/roles/{id}", DeleteRole).WithName("DeleteRole").WithSummary("Delete role");

      // ── ROLE PERMISSIONS ─────────────────────────────────────
      group.MapGet("/roles/{roleId}/permissions", GetRolePermissions).WithName("GetRolePermissions").WithSummary("Get role permissions");
      group.MapPost("/roles/{roleId}/permissions", AssignPermissionToRole).WithName("AssignPermissionToRole").WithSummary("Assign permission to role");
      group.MapDelete("/roles/{roleId}/permissions/{permId}", RemovePermissionFromRole).WithName("RemovePermissionFromRole").WithSummary("Remove permission from role");
      group.MapPut("/roles/{roleId}/permissions", SyncRolePermissions).WithName("SyncRolePermissions").WithSummary("Bulk sync role permissions");

      // ── USERS CRUD ───────────────────────────────────────────
      group.MapGet("/users", GetAllUsers).WithName("GetAllUsers").WithSummary("Get all users");
      group.MapGet("/users/{id}", GetUserById).WithName("GetUserById").WithSummary("Get specific user by ID");
      group.MapPost("/users", CreateUser).WithName("CreateUser").WithSummary("Create new user");
      group.MapPut("/users/{id}", UpdateUser).WithName("UpdateUser").WithSummary("Update user");
      group.MapDelete("/users/{id}", DeleteUser).WithName("DeleteUser").WithSummary("Delete user");

      // ── USER UNIT ROLES ──────────────────────────────────────
      group.MapGet("/users/{userId}/unit-roles", GetUserUnitRoles).WithName("GetUserUnitRoles").WithSummary("Get user unit roles");
      group.MapPost("/users/{userId}/unit-roles", AssignUnitRoleToUser).WithName("AssignUnitRoleToUser").WithSummary("Assign unit role");
      group.MapPost("/users/{userId}/unit-roles/revoke", RevokeUnitRoleFromUser).WithName("RevokeUnitRoleFromUser").WithSummary("Revoke unit role");

      // ── USER DIRECT PERMISSIONS ──────────────────────────────
      group.MapGet("/users/{userId}/direct-permissions", GetUserDirectPermissions).WithName("GetUserDirectPermissions").WithSummary("Get direct permissions");
      group.MapPost("/users/{userId}/direct-permissions", GrantDirectPermissionToUser).WithName("GrantDirectPermissionToUser").WithSummary("Grant direct permission");
      group.MapDelete("/users/{userId}/direct-permissions/{permId}", RevokeDirectPermissionFromUser).WithName("RevokeDirectPermissionFromUser").WithSummary("Revoke direct permission");

      // ── USER EFFECTIVE PERMISSIONS ───────────────────────────
      group.MapGet("/users/{userId}/permissions", GetUserEffectivePermissions).WithName("GetUserEffectivePermissions").WithSummary("Get effective permissions");

      // ── ORG UNITS ────────────────────────────────────────────
      group.MapGet("/org-units", GetAllOrgUnits).WithName("GetAllOrgUnits").WithSummary("Get all org units");
      group.MapGet("/org-units/tree", GetOrgUnitTree).WithName("GetOrgUnitTree").WithSummary("Get org unit tree");
      group.MapGet("/org-units/{id}", GetOrgUnitById).WithName("GetOrgUnitById").WithSummary("Get org unit by ID");
      group.MapPost("/org-units", CreateOrgUnit).WithName("CreateOrgUnit").WithSummary("Create org unit");
      group.MapPut("/org-units/{id}", UpdateOrgUnit).WithName("UpdateOrgUnit").WithSummary("Update org unit");
      group.MapDelete("/org-units/{id}", DeleteOrgUnit).WithName("DeleteOrgUnit").WithSummary("Delete org unit");

      // ── AUDIT LOGS ───────────────────────────────────────────
      group.MapGet("/audit-logs", GetAuditLogs).WithName("GetAuditLogs").WithSummary("Get paginated audit logs");
      group.MapGet("/audit-logs/export", ExportAuditLogs).WithName("ExportAuditLogs").WithSummary("Export audit logs");

      // ── SESSIONS ─────────────────────────────────────────────
      group.MapGet("/sessions", GetActiveSessions).WithName("GetActiveSessions").WithSummary("Get active sessions");
      group.MapPost("/sessions/{sessionId}/revoke", RevokeSession).WithName("RevokeSession").WithSummary("Revoke session");
      group.MapPost("/sessions/revoke-all", RevokeAllSessions).WithName("RevokeAllSessions").WithSummary("Revoke all sessions");

      // ── SYSTEM STATUS ────────────────────────────────────────
      group.MapGet("/system-status", GetSystemStatus).WithName("GetSystemStatus").WithSummary("Get system status");

      return group.WithName("Administration").WithTags("Administration RBAC").RequireAuthorization();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════
    private static string GetUserId(HttpContext ctx) =>
      ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";

    // ═══════════════════════════════════════════════════════════════
    // OVERVIEW
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetOverviewStats(
      ISystemStatusService statusService, IUserAdminService userService,
      IRoleManagementService roleService, IAuditLogService auditService,
      IActiveSessionService sessionService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var stats = new AdminOverviewStats(
          ActiveUsers: await userService.GetActiveUsersCountAsync(),
          TotalUsers: await userService.GetTotalUsersCountAsync(),
          RolesCount: (await roleService.GetAllRolesAsync()).Count,
          AuditEventsLast7Days: await auditService.GetCountLast7DaysAsync(),
          ActiveSessionsCount: (await sessionService.GetActiveSessionsAsync()).Count,
          SystemUptime: statusService.GetUptime()
        );
        return Results.Ok(ApiResponse.Success(stats, "Overview statistics retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get overview stats", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve overview statistics"));
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // PERMISSIONS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetPermissions(IRoleManagementService roleService, ILogWriter logger)
    {
      try
      {
        // Get all permissions from all roles (deduplicated)
        var roles = await roleService.GetAllRolesAsync();
        var allPerms = new List<RolePermissionDto>();
        foreach (var role in roles)
        {
          var perms = await roleService.GetRolePermissionsAsync(role.Id);
          allPerms.AddRange(perms);
        }
        var uniquePerms = allPerms
          .GroupBy(p => p.PermissionId)
          .Select(g => g.First())
          .Select(p => new PermissionDto(p.PermissionId, p.Module, p.Resource, p.Action, null))
          .ToList();
        return Results.Ok(ApiResponse.Success(uniquePerms, "Permissions retrieved"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get permissions", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve permissions"));
      }
    }

    private static async Task<IResult> GetPermissionById(int id, ILogWriter logger)
    {
      await logger.LogActionAsync("Get permission by ID", "Administration", $"PermissionId: {id}");
      return Results.Ok(ApiResponse.Success<object>(null, "Permission retrieved"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ROLES
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetAllRoles(IRoleManagementService roleService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await roleService.GetAllRolesAsync(), "All roles retrieved successfully")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get all roles", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve roles")); }
    }

    private static async Task<IResult> GetRoleById(string id, IRoleManagementService roleService, ILogWriter logger)
    {
      try
      {
        var role = await roleService.GetRoleByIdAsync(id);
        return role == null ? Results.NotFound(ApiResponse.Error("Role not found")) : Results.Ok(ApiResponse.Success(role, "Role retrieved successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get role by ID", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve role")); }
    }

    private static async Task<IResult> CreateRole(CreateRoleRequest request, IRoleManagementService roleService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var role = await roleService.CreateRoleAsync(request, GetUserId(ctx));
        return Results.Created($"/api/v1/rbac/roles/{role.Id}", ApiResponse.Success(role, "Role created successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to create role", ex); return Results.BadRequest(ApiResponse.Error("Failed to create role")); }
    }

    private static async Task<IResult> UpdateRole(string id, UpdateRoleRequest request, IRoleManagementService roleService, ILogWriter logger, HttpContext ctx)
    {
      try { return Results.Ok(ApiResponse.Success(await roleService.UpdateRoleAsync(id, request, GetUserId(ctx)), "Role updated successfully")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to update role", ex); return Results.BadRequest(ApiResponse.Error("Failed to update role")); }
    }

    private static async Task<IResult> DeleteRole(string id, IRoleManagementService roleService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var result = await roleService.DeleteRoleAsync(id, GetUserId(ctx));
        return !result ? Results.NotFound(ApiResponse.Error("Role not found")) : Results.NoContent();
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to delete role", ex); return Results.BadRequest(ApiResponse.Error("Failed to delete role")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // ROLE PERMISSIONS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetRolePermissions(string roleId, IRoleManagementService roleService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await roleService.GetRolePermissionsAsync(roleId), "Role permissions retrieved successfully")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get role permissions", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve role permissions")); }
    }

    private static async Task<IResult> AssignPermissionToRole(string roleId, AssignRolePermissionRequest request, IRoleManagementService roleService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var perm = await roleService.AssignPermissionToRoleAsync(roleId, request.PermissionId, GetUserId(ctx));
        return Results.Created("", ApiResponse.Success(perm, "Permission assigned to role successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to assign permission to role", ex); return Results.BadRequest(ApiResponse.Error("Failed to assign permission")); }
    }

    private static async Task<IResult> RemovePermissionFromRole(string roleId, int permId, IRoleManagementService roleService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var result = await roleService.RemovePermissionFromRoleAsync(roleId, permId, GetUserId(ctx));
        return !result ? Results.NotFound(ApiResponse.Error("Permission not found in role")) : Results.NoContent();
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to remove permission from role", ex); return Results.BadRequest(ApiResponse.Error("Failed to remove permission")); }
    }

    private static async Task<IResult> SyncRolePermissions(string roleId, int[] permissionIds, IRoleManagementService roleService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        await roleService.SyncRolePermissionsAsync(roleId, permissionIds, GetUserId(ctx));
        return Results.Ok(ApiResponse.Success("Permissions synchronized successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to sync role permissions", ex); return Results.BadRequest(ApiResponse.Error("Failed to sync permissions")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // USERS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetAllUsers(IUserAdminService userService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await userService.GetAllUsersAsync(), "All users retrieved successfully")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get all users", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve users")); }
    }

    private static async Task<IResult> GetUserById(string id, IUserAdminService userService, ILogWriter logger)
    {
      try
      {
        var user = await userService.GetUserByIdAsync(id);
        return user == null ? Results.NotFound(ApiResponse.Error("User not found")) : Results.Ok(ApiResponse.Success(user, "User retrieved successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get user by ID", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve user")); }
    }

    private static async Task<IResult> CreateUser(CreateUserRequest request, IUserAdminService userService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var user = await userService.CreateUserAsync(request, GetUserId(ctx));
        return Results.Created($"/api/v1/rbac/users/{user.Id}", ApiResponse.Success(user, "User created successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to create user", ex); return Results.BadRequest(ApiResponse.Error("Failed to create user")); }
    }

    private static async Task<IResult> UpdateUser(string id, UpdateUserRequest request, IUserAdminService userService, ILogWriter logger, HttpContext ctx)
    {
      try { return Results.Ok(ApiResponse.Success(await userService.UpdateUserAsync(id, request, GetUserId(ctx)), "User updated successfully")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to update user", ex); return Results.BadRequest(ApiResponse.Error("Failed to update user")); }
    }

    private static async Task<IResult> DeleteUser(string id, IUserAdminService userService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var result = await userService.DeleteUserAsync(id, GetUserId(ctx));
        return !result ? Results.NotFound(ApiResponse.Error("User not found")) : Results.NoContent();
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to delete user", ex); return Results.BadRequest(ApiResponse.Error("Failed to delete user")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // USER UNIT ROLES
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetUserUnitRoles(string userId, IUserAdminService userService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await userService.GetUserUnitRolesAsync(userId), "User unit roles retrieved")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get user unit roles", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve unit roles")); }
    }

    private static async Task<IResult> AssignUnitRoleToUser(string userId, AssignUnitRoleRequest request, IUserAdminService userService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var dto = await userService.AssignUnitRoleAsync(userId, request, GetUserId(ctx));
        return Results.Created("", ApiResponse.Success(dto, "Role assigned to user in unit"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to assign unit role to user", ex); return Results.BadRequest(ApiResponse.Error("Failed to assign role")); }
    }

    private static async Task<IResult> RevokeUnitRoleFromUser(string userId, RevokeUnitRoleRequest request, IUserAdminService userService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var result = await userService.RevokeUnitRoleAsync(userId, request, GetUserId(ctx));
        return !result ? Results.NotFound(ApiResponse.Error("Role assignment not found")) : Results.NoContent();
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to revoke unit role from user", ex); return Results.BadRequest(ApiResponse.Error("Failed to revoke role")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // USER DIRECT PERMISSIONS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetUserDirectPermissions(string userId, IUserAdminService userService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await userService.GetUserDirectPermissionsAsync(userId), "User direct permissions retrieved")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get user direct permissions", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve direct permissions")); }
    }

    private static async Task<IResult> GrantDirectPermissionToUser(string userId, SetDirectPermissionRequest request, IUserAdminService userService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var dto = await userService.GrantDirectPermissionAsync(userId, request, GetUserId(ctx));
        return Results.Created("", ApiResponse.Success(dto, "Permission granted to user"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to grant permission to user", ex); return Results.BadRequest(ApiResponse.Error("Failed to grant permission")); }
    }

    private static async Task<IResult> RevokeDirectPermissionFromUser(string userId, int permId, IUserAdminService userService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var result = await userService.RevokeDirectPermissionAsync(userId, permId, GetUserId(ctx));
        return !result ? Results.NotFound(ApiResponse.Error("Permission not found")) : Results.NoContent();
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to revoke permission from user", ex); return Results.BadRequest(ApiResponse.Error("Failed to revoke permission")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // USER EFFECTIVE PERMISSIONS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetUserEffectivePermissions(string userId, IUserAdminService userService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await userService.GetUserEffectivePermissionsAsync(userId), "User effective permissions retrieved")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get user effective permissions", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve effective permissions")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // ORG UNITS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetAllOrgUnits(IOrganizationUnitService orgUnitService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await orgUnitService.GetAllUnitsAsync(), "All organization units retrieved")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get all organization units", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve organization units")); }
    }

    private static async Task<IResult> GetOrgUnitTree(IOrganizationUnitService orgUnitService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await orgUnitService.GetUnitTreeAsync(), "Organization unit tree retrieved")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get organization unit tree", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve organization unit tree")); }
    }

    private static async Task<IResult> GetOrgUnitById(int id, IOrganizationUnitService orgUnitService, ILogWriter logger)
    {
      try
      {
        var unit = await orgUnitService.GetUnitByIdAsync(id);
        return unit == null ? Results.NotFound(ApiResponse.Error("Organization unit not found")) : Results.Ok(ApiResponse.Success(unit, "Organization unit retrieved"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get organization unit by ID", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve organization unit")); }
    }

    private static async Task<IResult> CreateOrgUnit(CreateOrgUnitRequest request, IOrganizationUnitService orgUnitService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var unit = await orgUnitService.CreateUnitAsync(request, GetUserId(ctx));
        return Results.Created($"/api/v1/rbac/org-units/{unit.Id}", ApiResponse.Success(unit, "Organization unit created successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to create organization unit", ex); return Results.BadRequest(ApiResponse.Error("Failed to create organization unit")); }
    }

    private static async Task<IResult> UpdateOrgUnit(int id, UpdateOrgUnitRequest request, IOrganizationUnitService orgUnitService, ILogWriter logger, HttpContext ctx)
    {
      try { return Results.Ok(ApiResponse.Success(await orgUnitService.UpdateUnitAsync(id, request, GetUserId(ctx)), "Organization unit updated successfully")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to update organization unit", ex); return Results.BadRequest(ApiResponse.Error("Failed to update organization unit")); }
    }

    private static async Task<IResult> DeleteOrgUnit(int id, IOrganizationUnitService orgUnitService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var result = await orgUnitService.DeleteUnitAsync(id, GetUserId(ctx));
        return !result ? Results.NotFound(ApiResponse.Error("Organization unit not found")) : Results.NoContent();
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to delete organization unit", ex); return Results.BadRequest(ApiResponse.Error("Failed to delete organization unit")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // AUDIT LOGS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetAuditLogs(IAuditLogService auditService, ILogWriter logger, [AsParameters] AuditLogFilterParams filterParams, int page = 1, int pageSize = 20)
    {
      try
      {
        var filter = new AuditLogFilter(filterParams.Search, filterParams.Module, filterParams.Action, filterParams.Status, filterParams.UserId, filterParams.FromDate, filterParams.ToDate);
        return Results.Ok(ApiResponse.Success(await auditService.GetAuditLogsAsync(filter, page, pageSize), "Audit logs retrieved successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get audit logs", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve audit logs")); }
    }

    private static async Task<IResult> ExportAuditLogs(IAuditLogService auditService, ILogWriter logger, [AsParameters] AuditLogFilterParams filterParams, string format = "csv")
    {
      try
      {
        var filter = new AuditLogFilter(filterParams.Search, filterParams.Module, filterParams.Action, filterParams.Status, filterParams.UserId, filterParams.FromDate, filterParams.ToDate);
        var bytes = await auditService.ExportAuditLogsAsync(filter, format);
        var contentType = format.ToLower() == "csv" ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        await logger.LogActionAsync("Export audit logs", "AuditLog", $"Format: {format}, Size: {bytes.Length} bytes");
        return Results.File(bytes, contentType, $"audit_logs_{DateTime.Now:yyyyMMddHHmmss}.{format}");
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to export audit logs", ex); return Results.BadRequest(ApiResponse.Error("Failed to export audit logs")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // SESSIONS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetActiveSessions(IActiveSessionService sessionService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await sessionService.GetActiveSessionsAsync(), "Active sessions retrieved successfully")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get active sessions", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve active sessions")); }
    }

    private static async Task<IResult> RevokeSession(string sessionId, IActiveSessionService sessionService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var result = await sessionService.RevokeSessionAsync(sessionId, GetUserId(ctx));
        return !result ? Results.NotFound(ApiResponse.Error("Session not found")) : Results.Ok(ApiResponse.Success("Session revoked successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to revoke session", ex); return Results.BadRequest(ApiResponse.Error("Failed to revoke session")); }
    }

    private static async Task<IResult> RevokeAllSessions(IActiveSessionService sessionService, ILogWriter logger, HttpContext ctx)
    {
      try
      {
        var currentSessionId = ctx.User.FindFirst("SessionId")?.Value;
        var count = await sessionService.RevokeAllSessionsExceptCurrentAsync(currentSessionId, GetUserId(ctx));
        return Results.Ok(ApiResponse.Success($"{count} sessions revoked successfully"));
      }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to revoke all sessions", ex); return Results.BadRequest(ApiResponse.Error("Failed to revoke sessions")); }
    }

    // ═══════════════════════════════════════════════════════════════
    // SYSTEM STATUS
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetSystemStatus(ISystemStatusService statusService, ILogWriter logger)
    {
      try { return Results.Ok(ApiResponse.Success(await statusService.GetSystemStatusAsync(), "System status retrieved successfully")); }
      catch (Exception ex) { await logger.LogErrorAsync("Failed to get system status", ex); return Results.BadRequest(ApiResponse.Error("Failed to retrieve system status")); }
    }

    public record AuditLogFilterParams(string? Search, string? Module, string? Action, string? Status, string? UserId, DateTime? FromDate, DateTime? ToDate);
  }
}

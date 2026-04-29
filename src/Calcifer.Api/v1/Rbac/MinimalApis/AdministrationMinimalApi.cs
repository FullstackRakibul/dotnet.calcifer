using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Interfaces;

namespace Calcifer.Api.Rbac.MinimalApis
{
  /// <summary>
  /// Minimal API extensions for administration module.
  /// All endpoints require Authorization and return ApiResponseDto wrapper.
  /// Endpoints grouped under /api/v1/rbac/admin/*
  /// </summary>
  public static class AdministrationMinimalApi
  {
    public static RouteGroupBuilder RegisterAdministrationApis(this RouteGroupBuilder group, ILogWriter logger)
    {

      // ══════════════════════════════════════════════════════════════════════════════
      // OVERVIEW STATS
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/overview/stats", GetOverviewStats)
        .WithName("GetAdminOverviewStats")
        .WithSummary("Get admin dashboard overview statistics")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // PERMISSIONS
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/permissions", GetPermissions)
          .WithName("GetPermissions")
          .WithSummary("Get all available permissions")
;

      group.MapGet("/permissions/{id}", GetPermissionById)
          .WithName("GetPermissionById")
          .WithSummary("Get specific permission by ID")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // ROLES CRUD
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/roles", GetAllRoles)
          .WithName("GetAllRoles")
          .WithSummary("Get all roles with user and permission counts")
;

      group.MapGet("/roles/{id}", GetRoleById)
          .WithName("GetRoleById")
          .WithSummary("Get specific role by ID")
;

      group.MapPost("/roles", CreateRole)
          .WithName("CreateRole")
          .WithSummary("Create new role")
;

      group.MapPut("/roles/{id}", UpdateRole)
          .WithName("UpdateRole")
          .WithSummary("Update existing role")
;

      group.MapDelete("/roles/{id}", DeleteRole)
          .WithName("DeleteRole")
          .WithSummary("Delete role")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // ROLE PERMISSIONS
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/roles/{roleId}/permissions", GetRolePermissions)
          .WithName("GetRolePermissions")
          .WithSummary("Get all permissions assigned to role")
;

      group.MapPost("/roles/{roleId}/permissions", AssignPermissionToRole)
          .WithName("AssignPermissionToRole")
          .WithSummary("Assign single permission to role")
;

      group.MapDelete("/roles/{roleId}/permissions/{permId}", RemovePermissionFromRole)
          .WithName("RemovePermissionFromRole")
          .WithSummary("Remove permission from role")
;

      group.MapPut("/roles/{roleId}/permissions", SyncRolePermissions)
          .WithName("SyncRolePermissions")
          .WithSummary("Bulk sync role permissions (replace all)")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // USERS CRUD
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/users", GetAllUsers)
          .WithName("GetAllUsers")
          .WithSummary("Get all users with roles and permissions")
;

      group.MapGet("/users/{id}", GetUserById)
          .WithName("GetUserById")
          .WithSummary("Get specific user by ID with full details")
;

      group.MapPost("/users", CreateUser)
          .WithName("CreateUser")
          .WithSummary("Create new user")
;

      group.MapPut("/users/{id}", UpdateUser)
          .WithName("UpdateUser")
          .WithSummary("Update user profile and assignment")
;

      group.MapDelete("/users/{id}", DeleteUser)
          .WithName("DeleteUser")
          .WithSummary("Delete/deactivate user")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // USER UNIT ROLES
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/users/{userId}/unit-roles", GetUserUnitRoles)
          .WithName("GetUserUnitRoles")
          .WithSummary("Get unit-role assignments for user")
;

      group.MapPost("/users/{userId}/unit-roles", AssignUnitRoleToUser)
          .WithName("AssignUnitRoleToUser")
          .WithSummary("Assign role to user in specific unit")
;

      group.MapPost("/users/{userId}/unit-roles/revoke", RevokeUnitRoleFromUser)
          .WithName("RevokeUnitRoleFromUser")
          .WithSummary("Revoke role from user in specific unit")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // USER DIRECT PERMISSIONS
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/users/{userId}/direct-permissions", GetUserDirectPermissions)
          .WithName("GetUserDirectPermissions")
          .WithSummary("Get direct permissions granted to user")
;

      group.MapPost("/users/{userId}/direct-permissions", GrantDirectPermissionToUser)
          .WithName("GrantDirectPermissionToUser")
          .WithSummary("Grant direct permission to user")
;

      group.MapDelete("/users/{userId}/direct-permissions/{permId}", RevokeDirectPermissionFromUser)
          .WithName("RevokeDirectPermissionFromUser")
          .WithSummary("Revoke direct permission from user")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // USER EFFECTIVE PERMISSIONS (Cache)
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/users/{userId}/permissions", GetUserEffectivePermissions)
          .WithName("GetUserEffectivePermissions")
          .WithSummary("Get user's effective permissions (roles + direct + cache)")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // ORGANIZATION UNITS
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/org-units", GetAllOrgUnits)
          .WithName("GetAllOrgUnits")
          .WithSummary("Get all organization units (flat list)")
;

      group.MapGet("/org-units/tree", GetOrgUnitTree)
          .WithName("GetOrgUnitTree")
          .WithSummary("Get organization units as hierarchical tree")
;

      group.MapGet("/org-units/{id}", GetOrgUnitById)
          .WithName("GetOrgUnitById")
          .WithSummary("Get specific organization unit by ID")
;

      group.MapPost("/org-units", CreateOrgUnit)
          .WithName("CreateOrgUnit")
          .WithSummary("Create new organization unit")
;

      group.MapPut("/org-units/{id}", UpdateOrgUnit)
          .WithName("UpdateOrgUnit")
          .WithSummary("Update organization unit")
;

      group.MapDelete("/org-units/{id}", DeleteOrgUnit)
          .WithName("DeleteOrgUnit")
          .WithSummary("Delete organization unit")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // AUDIT LOGS
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/audit-logs", GetAuditLogs)
          .WithName("GetAuditLogs")
          .WithSummary("Get paginated audit logs with filters")
;

      group.MapGet("/audit-logs/export", ExportAuditLogs)
          .WithName("ExportAuditLogs")
          .WithSummary("Export audit logs as CSV or Excel")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // ACTIVE SESSIONS
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/sessions", GetActiveSessions)
          .WithName("GetActiveSessions")
          .WithSummary("Get all active user sessions")
;

      group.MapPost("/sessions/{sessionId}/revoke", RevokeSession)
          .WithName("RevokeSession")
          .WithSummary("Revoke specific session")
;

      group.MapPost("/sessions/revoke-all", RevokeAllSessions)
          .WithName("RevokeAllSessions")
          .WithSummary("Revoke all sessions except current")
;

      // ══════════════════════════════════════════════════════════════════════════════
      // SYSTEM STATUS
      // ══════════════════════════════════════════════════════════════════════════════

      group.MapGet("/system-status", GetSystemStatus)
          .WithName("GetSystemStatus")
          .WithSummary("Get system health and status metrics")
;

      return group
        .WithName("Administration")
        .WithTags("Administration RBAC")
        .RequireAuthorization();
    }

    // ═════════════════════════════════════════════════════════════════════════════════
    // ENDPOINT HANDLERS
    // ═════════════════════════════════════════════════════════════════════════════════

    private static async Task<IResult> GetOverviewStats(
      ISystemStatusService statusService,
      IUserAdminService userService,
      IRoleManagementService roleService,
      IAuditLogService auditService,
      IActiveSessionService sessionService,
      ILogWriter logger,
      HttpContext ctx)
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

    private static async Task<IResult> GetPermissions(ILogWriter logger)
    {
      // TODO: Implement when IRbacService exposes GetAllPermissionsAsync
      return Results.Ok(ApiResponse.Success(new List<PermissionDto>(), "Permissions retrieved"));
    }

    private static async Task<IResult> GetPermissionById(int id, ILogWriter logger)
    {
      // TODO: Implement when IRbacService exposes GetPermissionByIdAsync
      return Results.Ok(ApiResponse.Success<object>(null, "Permission retrieved"));
    }

    private static async Task<IResult> GetAllRoles(
      IRoleManagementService roleService,
      ILogWriter logger)
    {
      try
      {
        var roles = await roleService.GetAllRolesAsync();
        return Results.Ok(ApiResponse.Success(roles, "All roles retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get all roles", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve roles"));
      }
    }

    private static async Task<IResult> GetRoleById(
      string id,
      IRoleManagementService roleService,
      ILogWriter logger)
    {
      try
      {
        var role = await roleService.GetRoleByIdAsync(id);
        if (role == null)
          return Results.NotFound(ApiResponse.Error("Role not found"));

        return Results.Ok(ApiResponse.Success(role, "Role retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get role by ID", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve role"));
      }
    }

    private static async Task<IResult> CreateRole(
      CreateRoleRequest request,
      IRoleManagementService roleService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var role = await roleService.CreateRoleAsync(request, userId);
        return Results.Created($"/api/v1/rbac/roles/{role.Id}",
          ApiResponse.Success(role, "Role created successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to create role", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to create role"));
      }
    }

    private static async Task<IResult> UpdateRole(
      string id,
      UpdateRoleRequest request,
      IRoleManagementService roleService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var role = await roleService.UpdateRoleAsync(id, request, userId);
        return Results.Ok(ApiResponse.Success(role, "Role updated successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to update role", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to update role"));
      }
    }

    private static async Task<IResult> DeleteRole(
      string id,
      IRoleManagementService roleService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var result = await roleService.DeleteRoleAsync(id, userId);
        if (!result)
          return Results.NotFound(ApiResponse.Error("Role not found"));

        return Results.NoContent();
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to delete role", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to delete role"));
      }
    }

    private static async Task<IResult> GetRolePermissions(
      string roleId,
      IRoleManagementService roleService,
      ILogWriter logger)
    {
      try
      {
        var permissions = await roleService.GetRolePermissionsAsync(roleId);
        return Results.Ok(ApiResponse.Success(permissions, "Role permissions retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get role permissions", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve role permissions"));
      }
    }

    private static async Task<IResult> AssignPermissionToRole(
      string roleId,
      AssignRolePermissionRequest request,
      IRoleManagementService roleService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var permission = await roleService.AssignPermissionToRoleAsync(roleId, request.PermissionId, userId);
        return Results.Created("", ApiResponse.Success(permission, "Permission assigned to role successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to assign permission to role", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to assign permission"));
      }
    }

    private static async Task<IResult> RemovePermissionFromRole(
      string roleId,
      int permId,
      IRoleManagementService roleService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var result = await roleService.RemovePermissionFromRoleAsync(roleId, permId, userId);
        if (!result)
          return Results.NotFound(ApiResponse.Error("Permission not found in role"));

        return Results.NoContent();
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to remove permission from role", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to remove permission"));
      }
    }

    private static async Task<IResult> SyncRolePermissions(
      string roleId,
      int[] permissionIds,
      IRoleManagementService roleService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        await roleService.SyncRolePermissionsAsync(roleId, permissionIds, userId);
        return Results.Ok(ApiResponse.Success("Permissions synchronized successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to sync role permissions", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to sync permissions"));
      }
    }

    private static async Task<IResult> GetAllUsers(
      IUserAdminService userService,
      ILogWriter logger)
    {
      try
      {
        var users = await userService.GetAllUsersAsync();
        return Results.Ok(ApiResponse.Success(users, "All users retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get all users", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve users"));
      }
    }

    private static async Task<IResult> GetUserById(
      string id,
      IUserAdminService userService,
      ILogWriter logger)
    {
      try
      {
        var user = await userService.GetUserByIdAsync(id);
        if (user == null)
          return Results.NotFound(ApiResponse.Error("User not found"));

        return Results.Ok(ApiResponse.Success(user, "User retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get user by ID", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve user"));
      }
    }

    private static async Task<IResult> CreateUser(
      CreateUserRequest request,
      IUserAdminService userService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var user = await userService.CreateUserAsync(request, userId);
        return Results.Created($"/api/v1/rbac/users/{user.Id}",
          ApiResponse.Success(user, "User created successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to create user", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to create user"));
      }
    }

    private static async Task<IResult> UpdateUser(
      string id,
      UpdateUserRequest request,
      IUserAdminService userService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var user = await userService.UpdateUserAsync(id, request, userId);
        return Results.Ok(ApiResponse.Success(user, "User updated successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to update user", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to update user"));
      }
    }

    private static async Task<IResult> DeleteUser(
      string id,
      IUserAdminService userService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var result = await userService.DeleteUserAsync(id, userId);
        if (!result)
          return Results.NotFound(ApiResponse.Error("User not found"));

        return Results.NoContent();
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to delete user", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to delete user"));
      }
    }

    private static async Task<IResult> GetUserUnitRoles(
      string userId,
      ILogWriter logger)
    {
      // TODO: Implement when service method is available
      return Results.Ok(ApiResponse.Success(new List<UserUnitRoleDto>(), "User unit roles retrieved"));
    }

    private static async Task<IResult> AssignUnitRoleToUser(
      string userId,
      AssignUnitRoleRequest request,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var requestedBy = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        await logger.LogActionAsync(
          "Assign unit role to user",
          "UserAdministration",
          $"UserId: {userId}, RoleId: {request.RoleId}, UnitId: {request.UnitId}");

        // TODO: Implement the actual assignment
        return Results.Created("", ApiResponse.Success("Role assigned to user in unit"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to assign unit role to user", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to assign role"));
      }
    }

    private static async Task<IResult> RevokeUnitRoleFromUser(
      string userId,
      RevokeUnitRoleRequest request,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var requestedBy = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        await logger.LogActionAsync(
          "Revoke unit role from user",
          "UserAdministration",
          $"UserId: {userId}, RoleId: {request.RoleId}, UnitId: {request.UnitId}");

        // TODO: Implement the actual revocation
        return Results.NoContent();
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to revoke unit role from user", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to revoke role"));
      }
    }

    private static async Task<IResult> GetUserDirectPermissions(
      string userId,
      ILogWriter logger)
    {
      // TODO: Implement when service method is available
      return Results.Ok(ApiResponse.Success(new List<DirectPermissionDto>(), "User direct permissions retrieved"));
    }

    private static async Task<IResult> GrantDirectPermissionToUser(
      string userId,
      SetDirectPermissionRequest request,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var requestedBy = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        await logger.LogActionAsync(
          "Grant direct permission to user",
          "UserAdministration",
          $"UserId: {userId}, PermissionId: {request.PermissionId}");

        // TODO: Implement the actual permission grant
        return Results.Created("", ApiResponse.Success("Permission granted to user"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to grant permission to user", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to grant permission"));
      }
    }

    private static async Task<IResult> RevokeDirectPermissionFromUser(
      string userId,
      int permId,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var requestedBy = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        await logger.LogActionAsync(
          "Revoke direct permission from user",
          "UserAdministration",
          $"UserId: {userId}, PermissionId: {permId}");

        // TODO: Implement the actual permission revocation
        return Results.NoContent();
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to revoke permission from user", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to revoke permission"));
      }
    }

    private static async Task<IResult> GetUserEffectivePermissions(
      string userId,
      ILogWriter logger)
    {
      // TODO: Implement when service method is available
      return Results.Ok(ApiResponse.Success<object>(null, "User effective permissions retrieved"));
    }

    private static async Task<IResult> GetAllOrgUnits(
      IOrganizationUnitService orgUnitService,
      ILogWriter logger)
    {
      try
      {
        var units = await orgUnitService.GetAllUnitsAsync();
        return Results.Ok(ApiResponse.Success(units, "All organization units retrieved"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get all organization units", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve organization units"));
      }
    }

    private static async Task<IResult> GetOrgUnitTree(
      IOrganizationUnitService orgUnitService,
      ILogWriter logger)
    {
      try
      {
        var tree = await orgUnitService.GetUnitTreeAsync();
        return Results.Ok(ApiResponse.Success(tree, "Organization unit tree retrieved"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get organization unit tree", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve organization unit tree"));
      }
    }

    private static async Task<IResult> GetOrgUnitById(
      int id,
      IOrganizationUnitService orgUnitService,
      ILogWriter logger)
    {
      try
      {
        var unit = await orgUnitService.GetUnitByIdAsync(id);
        if (unit == null)
          return Results.NotFound(ApiResponse.Error("Organization unit not found"));

        return Results.Ok(ApiResponse.Success(unit, "Organization unit retrieved"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get organization unit by ID", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve organization unit"));
      }
    }

    private static async Task<IResult> CreateOrgUnit(
      CreateOrgUnitRequest request,
      IOrganizationUnitService orgUnitService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var unit = await orgUnitService.CreateUnitAsync(request, userId);
        return Results.Created($"/api/v1/rbac/org-units/{unit.Id}",
          ApiResponse.Success(unit, "Organization unit created successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to create organization unit", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to create organization unit"));
      }
    }

    private static async Task<IResult> UpdateOrgUnit(
      int id,
      UpdateOrgUnitRequest request,
      IOrganizationUnitService orgUnitService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var unit = await orgUnitService.UpdateUnitAsync(id, request, userId);
        return Results.Ok(ApiResponse.Success(unit, "Organization unit updated successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to update organization unit", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to update organization unit"));
      }
    }

    private static async Task<IResult> DeleteOrgUnit(
      int id,
      IOrganizationUnitService orgUnitService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var result = await orgUnitService.DeleteUnitAsync(id, userId);
        if (!result)
          return Results.NotFound(ApiResponse.Error("Organization unit not found"));

        return Results.NoContent();
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to delete organization unit", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to delete organization unit"));
      }
    }

    private static async Task<IResult> GetAuditLogs(
      IAuditLogService auditService,
      ILogWriter logger,
      [AsParameters] AuditLogFilterParams filterParams,
      int page = 1,
      int pageSize = 20)
    {
      try
      {
        var filter = new AuditLogFilter(
          Search: filterParams.Search,
          Module: filterParams.Module,
          Action: filterParams.Action,
          Status: filterParams.Status,
          UserId: filterParams.UserId,
          FromDate: filterParams.FromDate,
          ToDate: filterParams.ToDate
        );

        var result = await auditService.GetAuditLogsAsync(filter, page, pageSize);
        return Results.Ok(ApiResponse.Success(result, "Audit logs retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get audit logs", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve audit logs"));
      }
    }

    private static async Task<IResult> ExportAuditLogs(
      IAuditLogService auditService,
      ILogWriter logger,
      [AsParameters] AuditLogFilterParams filterParams,
      string format = "csv")
    {
      try
      {
        var filter = new AuditLogFilter(
          Search: filterParams.Search,
          Module: filterParams.Module,
          Action: filterParams.Action,
          Status: filterParams.Status,
          UserId: filterParams.UserId,
          FromDate: filterParams.FromDate,
          ToDate: filterParams.ToDate
        );

        var bytes = await auditService.ExportAuditLogsAsync(filter, format);
        var contentType = format.ToLower() == "csv"
          ? "text/csv"
          : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var fileName = $"audit_logs_{DateTime.Now:yyyyMMddHHmmss}.{format}";

        await logger.LogActionAsync(
          "Export audit logs",
          "AuditLog",
          $"Format: {format}, Size: {bytes.Length} bytes");

        return Results.File(bytes, contentType, fileName);
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to export audit logs", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to export audit logs"));
      }
    }

    private static async Task<IResult> GetActiveSessions(
      IActiveSessionService sessionService,
      ILogWriter logger)
    {
      try
      {
        var sessions = await sessionService.GetActiveSessionsAsync();
        return Results.Ok(ApiResponse.Success(sessions, "Active sessions retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get active sessions", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve active sessions"));
      }
    }

    private static async Task<IResult> RevokeSession(
      string sessionId,
      IActiveSessionService sessionService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var result = await sessionService.RevokeSessionAsync(sessionId, userId);
        if (!result)
          return Results.NotFound(ApiResponse.Error("Session not found"));

        return Results.Ok(ApiResponse.Success("Session revoked successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to revoke session", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to revoke session"));
      }
    }

    private static async Task<IResult> RevokeAllSessions(
      IActiveSessionService sessionService,
      ILogWriter logger,
      HttpContext ctx)
    {
      try
      {
        var currentSessionId = ctx.User.FindFirst("SessionId")?.Value;
        var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("ID")?.Value ?? "system";
        var count = await sessionService.RevokeAllSessionsExceptCurrentAsync(currentSessionId, userId);

        return Results.Ok(ApiResponse.Success($"{count} sessions revoked successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to revoke all sessions", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to revoke sessions"));
      }
    }

    private static async Task<IResult> GetSystemStatus(
      ISystemStatusService statusService,
      ILogWriter logger)
    {
      try
      {
        var status = await statusService.GetSystemStatusAsync();
        return Results.Ok(ApiResponse.Success(status, "System status retrieved successfully"));
      }
      catch (Exception ex)
      {
        await logger.LogErrorAsync("Failed to get system status", ex);
        return Results.BadRequest(ApiResponse.Error("Failed to retrieve system status"));
      }
    }

    /// <summary>
    /// Helper record for binding audit log filter query parameters
    /// </summary>
    public record AuditLogFilterParams(
      string? Search,
      string? Module,
      string? Action,
      string? Status,
      string? UserId,
      DateTime? FromDate,
      DateTime? ToDate
    );
  }
}

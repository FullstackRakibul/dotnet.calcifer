using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Entities;
using Calcifer.Api.Rbac.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.Services
{
  /// <summary>
  /// Service for managing roles and role-permission assignments.
  /// Integrates with ASP.NET Identity RoleManager and EF Core.
  /// </summary>
  public class RoleManagementService : IRoleManagementService
  {
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly CalciferAppDbContext _dbContext;
    private readonly ILogWriter _logger;

    public RoleManagementService(
      RoleManager<ApplicationRole> roleManager,
      CalciferAppDbContext dbContext,
      ILogWriter logger)
    {
      _roleManager = roleManager;
      _dbContext = dbContext;
      _logger = logger;
    }

    // ═════════════════════════════════════════════════════════════════════
    // ROLES CRUD
    // ═════════════════════════════════════════════════════════════════════

    public async Task<List<RoleDto>> GetAllRolesAsync(CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get all roles",
          "RoleManagement",
          "Retrieving all system roles",
          _logger.GetCorrelationId());

        var roles = await _dbContext.Roles
          .Select(r => new RoleDto(
            r.Id,                       // Id
            r.Name ?? "",               // Name
            r.Description ?? "",        // Description
            r.IsSystemRole,             // IsSystem
            r.Users.Count,              // UsersCount
            r.RolePermissions.Count,    // PermissionCount
            DateTime.UtcNow             // LastUpdated
          ))
          .ToListAsync(ct);

        return roles;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get all roles", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<RoleDto?> GetRoleByIdAsync(string id, CancellationToken ct = default)
    {
      try
      {
        var role = await _dbContext.Roles
          .Where(r => r.Id == id)
          .Select(r => new RoleDto(
            r.Id,                       // Id
            r.Name ?? "",               // Name
            r.Description ?? "",        // Description
            r.IsSystemRole,             // IsSystem
            r.Users.Count,              // UsersCount
            r.RolePermissions.Count,    // PermissionCount
            DateTime.UtcNow             // LastUpdated
          ))
          .FirstOrDefaultAsync(ct);

        if (role == null)
        {
          await _logger.LogValidationAsync(
            "Get role by ID",
            "Not found",
            $"RoleId: {id}",
            _logger.GetCorrelationId());
          return null;
        }

        await _logger.LogActionAsync(
          "Get role by ID",
          "RoleManagement",
          $"Retrieved role: {role.Name}",
          _logger.GetCorrelationId());

        return role;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed to get role by ID: {id}", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, string createdBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Create role",
          "RoleManagement",
          $"Creating role: {request.Name}",
          _logger.GetCorrelationId());

        var role = new ApplicationRole
        {
          Name = request.Name,
          NormalizedName = request.Name.ToUpper(),
          Description = request.Description
        };

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
          var errors = string.Join(", ", result.Errors.Select(e => e.Description));
          await _logger.LogErrorAsync($"Failed to create role: {errors}", null, _logger.GetCorrelationId());
          throw new Exception($"Failed to create role: {errors}");
        }

        await _logger.LogActionAsync(
          "Role created successfully",
          "RoleManagement",
          $"RoleId: {role.Id}, CreatedBy: {createdBy}",
          _logger.GetCorrelationId());

        return new RoleDto(
          Id: role.Id,
          Name: role.Name ?? "",
          Description: role.Description ?? "",
          IsSystem: false,
          UsersCount: 0,
          PermissionCount: 0,
          LastUpdated: DateTime.UtcNow
        );
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to create role", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<RoleDto> UpdateRoleAsync(string id, UpdateRoleRequest request, string updatedBy, CancellationToken ct = default)
    {
      try
      {
        var role = await _dbContext.Roles
          .Include(r => r.Users)
          .Include(r => r.RolePermissions)
          .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (role == null)
        {
          await _logger.LogValidationAsync(
            "Update role",
            "Not found",
            $"RoleId: {id}",
            _logger.GetCorrelationId());
          throw new Exception("Role not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
          role.Name = request.Name;

        if (request.Description != null)
          role.Description = request.Description;

        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
          var errors = string.Join(", ", result.Errors.Select(e => e.Description));
          throw new Exception($"Failed to update role: {errors}");
        }

        await _logger.LogActionAsync(
          "Role updated",
          "RoleManagement",
          $"RoleId: {id}, UpdatedBy: {updatedBy}",
          _logger.GetCorrelationId());

        return new RoleDto(
          Id: role.Id,
          Name: role.Name ?? "",
          Description: role.Description ?? "",
          IsSystem: role.IsSystemRole,
          UsersCount: role.Users.Count,
          PermissionCount: role.RolePermissions.Count,
          LastUpdated: DateTime.UtcNow
        );
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to update role", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<bool> DeleteRoleAsync(string id, string deletedBy, CancellationToken ct = default)
    {
      try
      {
        var role = await _dbContext.Roles
          .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (role == null)
        {
          await _logger.LogValidationAsync(
            "Delete role",
            "Not found",
            $"RoleId: {id}",
            _logger.GetCorrelationId());
          return false;
        }

        // Remove all permissions linked to this role
        var rolePermissions = _dbContext.RolePermissions.Where(rp => rp.RoleId == id);
        _dbContext.RolePermissions.RemoveRange(rolePermissions);

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
          var errors = string.Join(", ", result.Errors.Select(e => e.Description));
          throw new Exception($"Failed to delete role: {errors}");
        }

        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Role deleted",
          "RoleManagement",
          $"RoleId: {id}, DeletedBy: {deletedBy}",
          _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to delete role", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    // ═════════════════════════════════════════════════════════════════════
    // ROLE ↔ PERMISSIONS
    // ═════════════════════════════════════════════════════════════════════

    public async Task<List<RolePermissionDto>> GetRolePermissionsAsync(string roleId, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get role permissions",
          "RoleManagement",
          $"RoleId: {roleId}",
          _logger.GetCorrelationId());

        var permissions = await _dbContext.RolePermissions
          .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
          .Select(rp => new RolePermissionDto(
            rp.RoleId,                  // RoleId
            rp.Role.Name ?? "",         // RoleName
            rp.PermissionId,            // PermissionId
            rp.Permission.Module,       // Module
            rp.Permission.Resource,     // Resource
            rp.Permission.Action        // Action
          ))
          .ToListAsync(ct);

        return permissions;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get role permissions", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<RolePermissionDto> AssignPermissionToRoleAsync(string roleId, int permissionId, string assignedBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Assign permission to role",
          "RoleManagement",
          $"RoleId: {roleId}, PermissionId: {permissionId}, AssignedBy: {assignedBy}",
          _logger.GetCorrelationId());

        var exists = await _dbContext.RolePermissions
          .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && !rp.IsDeleted, ct);

        if (exists)
          throw new Exception("Role already has this permission.");

        var rolePermission = new RolePermission
        {
          RoleId = roleId,
          PermissionId = permissionId,
          CreatedBy = assignedBy,
          CreatedAt = DateTime.UtcNow,
          StatusId = 1
        };

        _dbContext.RolePermissions.Add(rolePermission);
        await _dbContext.SaveChangesAsync(ct);

        var permission = await _dbContext.Permissions.FindAsync(permissionId);
        var role = await _dbContext.Roles.FindAsync(roleId);

        await _logger.LogActionAsync(
          "Permission assigned to role",
          "RoleManagement",
          $"RoleId: {roleId}, PermissionId: {permissionId}",
          _logger.GetCorrelationId());

        return new RolePermissionDto(
          RoleId: roleId,
          RoleName: role?.Name ?? "",
          PermissionId: permissionId,
          Module: permission?.Module ?? "",
          Resource: permission?.Resource ?? "",
          Action: permission?.Action ?? ""
        );
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to assign permission to role", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(string roleId, int permissionId, string removedBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Remove permission from role",
          "RoleManagement",
          $"RoleId: {roleId}, PermissionId: {permissionId}, RemovedBy: {removedBy}",
          _logger.GetCorrelationId());

        var rp = await _dbContext.RolePermissions
          .FirstOrDefaultAsync(r => r.RoleId == roleId && r.PermissionId == permissionId && !r.IsDeleted, ct);

        if (rp == null) return false;

        rp.IsDeleted = true;
        rp.DeletedAt = DateTime.UtcNow;
        rp.DeletedBy = removedBy;

        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Permission removed from role",
          "RoleManagement",
          $"RoleId: {roleId}, PermissionId: {permissionId}",
          _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to remove permission from role", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task SyncRolePermissionsAsync(string roleId, int[] permissionIds, string updatedBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Sync role permissions",
          "RoleManagement",
          $"RoleId: {roleId}, PermissionCount: {permissionIds.Length}",
          _logger.GetCorrelationId());

        // Remove all current permissions
        var existing = _dbContext.RolePermissions.Where(rp => rp.RoleId == roleId);
        _dbContext.RolePermissions.RemoveRange(existing);

        // Add new permissions
        var newPermissions = permissionIds.Select(pId => new RolePermission
        {
          RoleId = roleId,
          PermissionId = pId,
          CreatedBy = updatedBy,
          CreatedAt = DateTime.UtcNow,
          StatusId = 1
        }).ToList();

        _dbContext.RolePermissions.AddRange(newPermissions);
        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Role permissions synced",
          "RoleManagement",
          $"RoleId: {roleId}, PermissionCount: {permissionIds.Length}, UpdatedBy: {updatedBy}",
          _logger.GetCorrelationId());
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to sync role permissions", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    // ═════════════════════════════════════════════════════════════════════
    // PERMISSION CHECKS
    // ═════════════════════════════════════════════════════════════════════

    public async Task<bool> HasPermissionAsync(string userId, string module, string resource, string action, CancellationToken ct = default)
    {
      try
      {
        var perms = await GetUserEffectivePermissionsAsync(userId, ct);
        var key = $"{module}:{resource}:{action}";

        return perms.Contains(key)
          || perms.Contains($"{module}:*:*")
          || perms.Contains("*:*:*");
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed permission check for user {userId}: {module}:{resource}:{action}", ex, _logger.GetCorrelationId());
        return false;
      }
    }

    public async Task<IReadOnlySet<string>> GetUserEffectivePermissionsAsync(string userId, CancellationToken ct = default)
    {
      try
      {
        var now = DateTime.UtcNow;

        // Step 1: Get active role IDs from UserUnitRoles
        var roleIds = await _dbContext.UserUnitRoles
          .Where(uur => uur.UserId == userId
                     && !uur.IsDeleted
                     && (uur.ValidTo == null || uur.ValidTo > now))
          .Select(uur => uur.RoleId)
          .Distinct()
          .ToListAsync(ct);

        // Step 2: Get permissions from those roles
        var roleKeys = await _dbContext.RolePermissions
          .Where(rp => roleIds.Contains(rp.RoleId) && !rp.IsDeleted)
          .Include(rp => rp.Permission)
          .Select(rp => $"{rp.Permission.Module}:{rp.Permission.Resource}:{rp.Permission.Action}")
          .ToListAsync(ct);

        var permSet = new HashSet<string>(roleKeys);

        // Step 3: Apply direct overrides
        var overrides = await _dbContext.UserDirectPermissions
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

        return permSet;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed to get effective permissions for user {userId}", ex, _logger.GetCorrelationId());
        return new HashSet<string>();
      }
    }
  }
}

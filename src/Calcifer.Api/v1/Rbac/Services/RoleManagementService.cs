using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
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
            Id: r.Id,
            Name: r.Name ?? "",
            Description: r.Description ?? "",
            IsSystem: false,
            UsersCount: r.Users.Count,
            PermissionCount: r.Permissions.Count,
            LastUpdated: DateTime.UtcNow
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
          .FirstOrDefaultAsync(r => r.Id == id, ct);

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

        var dto = new RoleDto(
          Id: role.Id,
          Name: role.Name ?? "",
          Description: role.Description ?? "",
          IsSystem: false,
          UsersCount: role.Users.Count,
          PermissionCount: role.Permissions.Count,
          LastUpdated: DateTime.UtcNow
        );

        return dto;
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
          IsSystem: false,
          UsersCount: role.Users.Count,
          PermissionCount: role.Permissions.Count,
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

    public async Task SyncRolePermissionsAsync(string roleId, int[] permissionIds, string updatedBy, CancellationToken ct = default)
    {
      try
      {
        // Remove all current permissions
        var existing = _dbContext.RolePermissions.Where(rp => rp.RoleId == roleId);
        _dbContext.RolePermissions.RemoveRange(existing);

        // Add new permissions
        var newPermissions = permissionIds.Select(pId => new RolePermission
        {
          RoleId = roleId,
          PermissionId = pId
        }).ToList();

        _dbContext.RolePermissions.AddRange(newPermissions);
        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Sync role permissions",
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
  }
}

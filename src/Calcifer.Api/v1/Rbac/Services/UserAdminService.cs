using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Common;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Entities;
using Calcifer.Api.Rbac.Interfaces;
using Calcifer.Api.Rbac.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.Services
{
  /// <summary>
  /// Service for admin user management.
  /// Handles user CRUD, unit role assignment, direct permissions, and effective permission summaries.
  /// </summary>
  public class UserAdminService : IUserAdminService
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CalciferAppDbContext _dbContext;
    private readonly IUserReadRepository _userReadRepository;
    private readonly ILogWriter _logger;

    public UserAdminService(
      UserManager<ApplicationUser> userManager,
      CalciferAppDbContext dbContext,
      IUserReadRepository userReadRepository,
      ILogWriter logger)
    {
      _userManager = userManager;
      _dbContext = dbContext;
      _userReadRepository = userReadRepository;
      _logger = logger;
    }

    // ═════════════════════════════════════════════════════════════════════
    // USERS CRUD
    // ═════════════════════════════════════════════════════════════════════

    public async Task<List<AdminUserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get all users for admin",
          "UserAdministration",
          "Retrieving all users with roles and permissions",
          _logger.GetCorrelationId());

        return await _userReadRepository.GetAllUsersAsync();
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get all users", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(string id, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get user by ID",
          "UserAdministration",
          $"UserId: {id}",
          _logger.GetCorrelationId());

        return await _userReadRepository.GetUserByIdAsync(id);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed to get user by ID: {id}", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<AdminUserDto> CreateUserAsync(CreateUserRequest request, string createdBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Create user",
          "UserAdministration",
          $"Email: {request.Email}, FirstName: {request.FirstName}",
          _logger.GetCorrelationId());

        var user = new ApplicationUser
        {
          UserName = request.Email,
          Email = request.Email,
          Name = $"{request.FirstName} {request.LastName}",
          FirstName = request.FirstName,
          LastName = request.LastName,
          PhoneNumber = request.Phone,
          Department = request.Department,
          BaseUnitId = request.BaseUnitId,
          EmailConfirmed = false,
          StatusId = 1,  // Active
          CreatedBy = createdBy
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
          var errors = string.Join(", ", result.Errors.Select(e => e.Description));
          await _logger.LogErrorAsync($"Failed to create user: {errors}", null, _logger.GetCorrelationId());
          throw new Exception($"Failed to create user: {errors}");
        }

        await _logger.LogActionAsync(
          "User created successfully",
          "UserAdministration",
          $"UserId: {user.Id}, CreatedBy: {createdBy}",
          _logger.GetCorrelationId());

        return await MapUserToAdminUserDtoAsync(user);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to create user", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<AdminUserDto> UpdateUserAsync(string id, UpdateUserRequest request, string updatedBy, CancellationToken ct = default)
    {
      try
      {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
          await _logger.LogValidationAsync(
            "Update user",
            "Not found",
            $"UserId: {id}",
            _logger.GetCorrelationId());
          throw new Exception("User not found");
        }

        if (!string.IsNullOrEmpty(request.FirstName))
        {
          user.FirstName = request.FirstName;
          user.Name = $"{request.FirstName} {user.LastName}";
        }

        if (!string.IsNullOrEmpty(request.LastName))
        {
          user.LastName = request.LastName;
          user.Name = $"{user.FirstName} {request.LastName}";
        }

        if (!string.IsNullOrEmpty(request.Phone))
          user.PhoneNumber = request.Phone;

        if (!string.IsNullOrEmpty(request.Department))
          user.Department = request.Department;

        if (request.BaseUnitId.HasValue)
          user.BaseUnitId = request.BaseUnitId;

        if (!string.IsNullOrEmpty(request.Status))
        {
          // Map string status to StatusId from CommonStatus table
          var status = await _dbContext.CommonStatus
            .FirstOrDefaultAsync(cs => cs.StatusName.ToLower() == request.Status.ToLower() && cs.Module == "User");
          if (status != null)
            user.StatusId = status.Id;
        }

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedBy;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
          var errors = string.Join(", ", result.Errors.Select(e => e.Description));
          throw new Exception($"Failed to update user: {errors}");
        }

        await _logger.LogActionAsync(
          "User updated",
          "UserAdministration",
          $"UserId: {id}, UpdatedBy: {updatedBy}",
          _logger.GetCorrelationId());

        return await MapUserToAdminUserDtoAsync(user);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to update user", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<bool> DeleteUserAsync(string id, string deletedBy, CancellationToken ct = default)
    {
      try
      {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
          await _logger.LogValidationAsync(
            "Delete user",
            "Not found",
            $"UserId: {id}",
            _logger.GetCorrelationId());
          return false;
        }

        // Soft delete - mark as deleted
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = deletedBy;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
          var errors = string.Join(", ", result.Errors.Select(e => e.Description));
          throw new Exception($"Failed to delete user: {errors}");
        }

        await _logger.LogActionAsync(
          "User deleted",
          "UserAdministration",
          $"UserId: {id}, DeletedBy: {deletedBy}",
          _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to delete user", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    // ═════════════════════════════════════════════════════════════════════
    // STATS (for overview dashboard)
    // ═════════════════════════════════════════════════════════════════════

    public async Task<int> GetActiveUsersCountAsync()
    {
      try
      {
        return await _userReadRepository.GetActiveUsersCountAsync();
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get active users count", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<int> GetTotalUsersCountAsync()
    {
      try
      {
        return await _userReadRepository.GetTotalUsersCountAsync();
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get total users count", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<PaginatedResponse<AdminUserDto>> SearchUsersAsync(string? search, int page = 1, int pageSize = 20)
    {
      try
      {
        await _logger.LogActionAsync(
          "Search users",
          "UserAdministration",
          $"Search: {search}, Page: {page}, PageSize: {pageSize}",
          _logger.GetCorrelationId());

        return await _userReadRepository.SearchUsersAsync(search, page, pageSize);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to search users", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    // ═════════════════════════════════════════════════════════════════════
    // USER UNIT ROLES
    // ═════════════════════════════════════════════════════════════════════

    public async Task<List<UserUnitRoleDto>> GetUserUnitRolesAsync(string userId, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get user unit roles",
          "UserAdministration",
          $"UserId: {userId}",
          _logger.GetCorrelationId());

        var now = DateTime.UtcNow;

        return await _dbContext.UserUnitRoles
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
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed to get user unit roles for: {userId}", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<UserUnitRoleDto> AssignUnitRoleAsync(string userId, AssignUnitRoleRequest request, string assignedBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Assign unit role to user",
          "UserAdministration",
          $"UserId: {userId}, RoleId: {request.RoleId}, UnitId: {request.UnitId}, AssignedBy: {assignedBy}",
          _logger.GetCorrelationId());

        var exists = await _dbContext.UserUnitRoles
          .AnyAsync(uur => uur.UserId == userId
                        && uur.RoleId == request.RoleId
                        && uur.UnitId == request.UnitId
                        && !uur.IsDeleted, ct);

        if (exists)
          throw new Exception("User already has this role at this unit.");

        var unitRole = new UserUnitRole
        {
          UserId = userId,
          RoleId = request.RoleId,
          UnitId = request.UnitId,
          ValidFrom = request.ValidFrom,
          ValidTo = request.ValidTo,
          CreatedBy = assignedBy,
          CreatedAt = DateTime.UtcNow,
          StatusId = 1
        };

        _dbContext.UserUnitRoles.Add(unitRole);
        await _dbContext.SaveChangesAsync(ct);

        // Load navigations for DTO
        var role = await _dbContext.Roles.FindAsync(request.RoleId);
        var unit = await _dbContext.OrganizationUnits.FindAsync(request.UnitId);

        await _logger.LogActionAsync(
          "Unit role assigned",
          "UserAdministration",
          $"UserId: {userId}, RoleId: {request.RoleId}, UnitId: {request.UnitId}",
          _logger.GetCorrelationId());

        var now = DateTime.UtcNow;
        return new UserUnitRoleDto(
          UserId: userId,
          RoleId: request.RoleId,
          RoleName: role?.Name ?? "",
          UnitId: request.UnitId,
          UnitName: unit?.Name ?? "",
          ValidFrom: request.ValidFrom,
          ValidTo: request.ValidTo,
          IsActive: (request.ValidFrom == null || request.ValidFrom <= now) &&
                   (request.ValidTo == null || request.ValidTo >= now)
        );
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to assign unit role to user", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<bool> RevokeUnitRoleAsync(string userId, RevokeUnitRoleRequest request, string revokedBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Revoke unit role from user",
          "UserAdministration",
          $"UserId: {userId}, RoleId: {request.RoleId}, UnitId: {request.UnitId}, RevokedBy: {revokedBy}",
          _logger.GetCorrelationId());

        var row = await _dbContext.UserUnitRoles
          .FirstOrDefaultAsync(uur => uur.UserId == userId
                                   && uur.RoleId == request.RoleId
                                   && uur.UnitId == request.UnitId
                                   && !uur.IsDeleted, ct);

        if (row == null) return false;

        row.IsDeleted = true;
        row.DeletedAt = DateTime.UtcNow;
        row.DeletedBy = revokedBy;

        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Unit role revoked",
          "UserAdministration",
          $"UserId: {userId}, RoleId: {request.RoleId}, UnitId: {request.UnitId}",
          _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to revoke unit role from user", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    // ═════════════════════════════════════════════════════════════════════
    // USER DIRECT PERMISSIONS
    // ═════════════════════════════════════════════════════════════════════

    public async Task<List<DirectPermissionDto>> GetUserDirectPermissionsAsync(string userId, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get user direct permissions",
          "UserAdministration",
          $"UserId: {userId}",
          _logger.GetCorrelationId());

        return await _dbContext.UserDirectPermissions
          .Where(udp => udp.UserId == userId && !udp.IsDeleted)
          .Select(udp => new DirectPermissionDto(
            udp.UserId,                 // UserId
            udp.PermissionId,           // PermissionId
            udp.Permission.Module,      // Module
            udp.Permission.Resource,    // Resource
            udp.Permission.Action,      // Action
            udp.IsGranted,              // IsGranted
            udp.GrantedBy,              // GrantedBy
            udp.ExpiresAt               // ExpiresAt
          ))
          .ToListAsync(ct);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed to get direct permissions for user: {userId}", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<DirectPermissionDto> GrantDirectPermissionAsync(string userId, SetDirectPermissionRequest request, string grantedBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Grant direct permission to user",
          "UserAdministration",
          $"UserId: {userId}, PermissionId: {request.PermissionId}, IsGranted: {request.IsGranted}, GrantedBy: {grantedBy}",
          _logger.GetCorrelationId());

        var existing = await _dbContext.UserDirectPermissions
          .IgnoreQueryFilters()
          .FirstOrDefaultAsync(udp => udp.UserId == userId && udp.PermissionId == request.PermissionId, ct);

        var now = DateTime.UtcNow;

        if (existing == null)
        {
          _dbContext.UserDirectPermissions.Add(new UserDirectPermission
          {
            UserId = userId,
            PermissionId = request.PermissionId,
            IsGranted = request.IsGranted,
            ExpiresAt = request.ExpiresAt,
            GrantedBy = grantedBy,
            CreatedAt = now,
            CreatedBy = grantedBy,
            IsDeleted = false,
            StatusId = 1
          });
        }
        else
        {
          existing.IsGranted = request.IsGranted;
          existing.ExpiresAt = request.ExpiresAt;
          existing.GrantedBy = grantedBy;
          existing.IsDeleted = false;
          existing.UpdatedAt = now;
          existing.UpdatedBy = grantedBy;
        }

        await _dbContext.SaveChangesAsync(ct);

        var permission = await _dbContext.Permissions.FindAsync(request.PermissionId);

        await _logger.LogActionAsync(
          "Direct permission granted",
          "UserAdministration",
          $"UserId: {userId}, PermissionId: {request.PermissionId}",
          _logger.GetCorrelationId());

        return new DirectPermissionDto(
          UserId: userId,
          PermissionId: request.PermissionId,
          Module: permission?.Module ?? "",
          Resource: permission?.Resource ?? "",
          Action: permission?.Action ?? "",
          IsGranted: request.IsGranted,
          GrantedBy: grantedBy,
          ExpiresAt: request.ExpiresAt
        );
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to grant direct permission to user", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<bool> RevokeDirectPermissionAsync(string userId, int permissionId, string revokedBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Revoke direct permission from user",
          "UserAdministration",
          $"UserId: {userId}, PermissionId: {permissionId}, RevokedBy: {revokedBy}",
          _logger.GetCorrelationId());

        var row = await _dbContext.UserDirectPermissions
          .FirstOrDefaultAsync(udp => udp.UserId == userId
                                   && udp.PermissionId == permissionId
                                   && !udp.IsDeleted, ct);

        if (row == null) return false;

        row.IsDeleted = true;
        row.DeletedAt = DateTime.UtcNow;
        row.DeletedBy = revokedBy;

        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Direct permission revoked",
          "UserAdministration",
          $"UserId: {userId}, PermissionId: {permissionId}",
          _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to revoke direct permission from user", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    // ═════════════════════════════════════════════════════════════════════
    // EFFECTIVE PERMISSIONS SUMMARY
    // ═════════════════════════════════════════════════════════════════════

    public async Task<UserPermissionSummary> GetUserEffectivePermissionsAsync(string userId, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get user effective permissions",
          "UserAdministration",
          $"UserId: {userId}",
          _logger.GetCorrelationId());

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        var now = DateTime.UtcNow;

        // Get user roles
        var roleNames = await _dbContext.UserUnitRoles
          .Where(uur => uur.UserId == userId && !uur.IsDeleted
                     && (uur.ValidTo == null || uur.ValidTo > now))
          .Include(uur => uur.Role)
          .Select(uur => uur.Role.Name ?? "")
          .Distinct()
          .ToListAsync(ct);

        // Get role IDs for permission lookup
        var roleIds = await _dbContext.UserUnitRoles
          .Where(uur => uur.UserId == userId && !uur.IsDeleted
                     && (uur.ValidTo == null || uur.ValidTo > now))
          .Select(uur => uur.RoleId)
          .Distinct()
          .ToListAsync(ct);

        // Get role-based permissions
        var rolePerms = await _dbContext.RolePermissions
          .Where(rp => roleIds.Contains(rp.RoleId) && !rp.IsDeleted)
          .Include(rp => rp.Permission)
          .Select(rp => $"{rp.Permission.Module}:{rp.Permission.Resource}:{rp.Permission.Action}")
          .ToListAsync(ct);

        var permSet = new HashSet<string>(rolePerms);

        // Get direct overrides
        var directOverrides = await _dbContext.UserDirectPermissions
          .Where(udp => udp.UserId == userId && !udp.IsDeleted
                     && (udp.ExpiresAt == null || udp.ExpiresAt > now))
          .Include(udp => udp.Permission)
          .ToListAsync(ct);

        var overrideDtos = new List<DirectPermissionDto>();
        foreach (var o in directOverrides)
        {
          var key = $"{o.Permission.Module}:{o.Permission.Resource}:{o.Permission.Action}";
          if (o.IsGranted) permSet.Add(key);
          else permSet.Remove(key);

          overrideDtos.Add(new DirectPermissionDto(
            UserId: o.UserId,
            PermissionId: o.PermissionId,
            Module: o.Permission.Module,
            Resource: o.Permission.Resource,
            Action: o.Permission.Action,
            IsGranted: o.IsGranted,
            GrantedBy: o.GrantedBy,
            ExpiresAt: o.ExpiresAt
          ));
        }

        // Check cache staleness
        var cache = await _dbContext.PermissionCache
          .AsNoTracking()
          .FirstOrDefaultAsync(pc => pc.UserId == userId && pc.UnitId == null, ct);

        var cacheGeneratedAt = cache?.GeneratedAt ?? DateTime.MinValue;
        var cacheIsStale = cache == null || cache.GeneratedAt < now.AddMinutes(-5);

        return new UserPermissionSummary(
          UserId: userId,
          UserName: $"{user.FirstName} {user.LastName}",
          Email: user.Email ?? "",
          Roles: roleNames,
          EffectivePermissions: permSet.OrderBy(k => k).ToList(),
          DirectOverrides: overrideDtos,
          CacheGeneratedAt: cacheGeneratedAt,
          CacheIsStale: cacheIsStale
        );
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed to get effective permissions for user: {userId}", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    // ═════════════════════════════════════════════════════════════════════
    // PRIVATE MAPPING
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Map ApplicationUser to AdminUserDto with hydrated roles and permissions
    /// </summary>
    private async Task<AdminUserDto> MapUserToAdminUserDtoAsync(ApplicationUser user)
    {
      var unitRoles = await _dbContext.UserUnitRoles
        .Where(ur => ur.UserId == user.Id && !ur.IsDeleted)
        .Select(ur => new UserUnitRoleDto(
          ur.UserId,                    // UserId
          ur.RoleId,                    // RoleId
          ur.Role.Name ?? "",           // RoleName
          ur.UnitId,                    // UnitId
          ur.Unit.Name ?? "",           // UnitName
          ur.ValidFrom,                 // ValidFrom
          ur.ValidTo,                   // ValidTo
          (ur.ValidFrom == null || ur.ValidFrom <= DateTime.UtcNow) &&
                    (ur.ValidTo == null || ur.ValidTo >= DateTime.UtcNow) // IsActive
        ))
        .ToListAsync();

      var directPermissions = await _dbContext.UserDirectPermissions
        .Where(udp => udp.UserId == user.Id && !udp.IsDeleted)
        .Select(udp => new DirectPermissionDto(
          udp.UserId,                   // UserId
          udp.PermissionId,             // PermissionId
          udp.Permission.Module,        // Module
          udp.Permission.Resource,      // Resource
          udp.Permission.Action,        // Action
          udp.IsGranted,                // IsGranted
          udp.GrantedBy,                // GrantedBy
          udp.ExpiresAt                 // ExpiresAt
        ))
        .ToListAsync();

      // Resolve status name from CommonStatus FK
      var statusName = "active";
      if (user.Status != null)
        statusName = user.Status.StatusName.ToLower();
      else if (user.StatusId > 0)
      {
        var status = await _dbContext.CommonStatus.FindAsync(user.StatusId);
        statusName = status?.StatusName.ToLower() ?? "active";
      }

      return new AdminUserDto(
        Id: user.Id,
        FirstName: user.FirstName ?? "",
        LastName: user.LastName ?? "",
        Email: user.Email ?? "",
        Phone: user.PhoneNumber,
        Department: user.Department,
        BaseUnitId: user.BaseUnitId,
        BaseUnitName: user.BaseUnit?.Name,
        Status: statusName,
        JoinedDate: user.CreatedAt,
        LastLogin: user.LastLogin,
        AvatarUrl: null,
        UnitRoles: unitRoles,
        DirectPermissions: directPermissions
      );
    }
  }
}

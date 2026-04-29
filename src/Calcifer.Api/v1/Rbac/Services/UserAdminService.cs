using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Interfaces;
using Calcifer.Api.Rbac.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Calcifer.Api.Rbac.Services
{
  /// <summary>
  /// Service for admin user management.
  /// Handles user CRUD and provides hydrated AdminUserDto with roles and permissions.
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
          FirstName = request.FirstName,
          LastName = request.LastName,
          PhoneNumber = request.Phone,
          Department = request.Department,
          BaseUnitId = request.BaseUnitId,
          EmailConfirmed = false,
          Status = "active"
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
          user.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
          user.LastName = request.LastName;

        if (!string.IsNullOrEmpty(request.Phone))
          user.PhoneNumber = request.Phone;

        if (!string.IsNullOrEmpty(request.Department))
          user.Department = request.Department;

        if (request.BaseUnitId.HasValue)
          user.BaseUnitId = request.BaseUnitId;

        if (!string.IsNullOrEmpty(request.Status))
          user.Status = request.Status;

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

        // Soft delete - mark as inactive
        user.Status = "deleted";
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
          var errors = string.Join(", ", result.Errors.Select(e => e.Description));
          throw new Exception($"Failed to delete user: {errors}");
        }

        await _logger.LogActionAsync(
          "User deleted",
          "UserAdministration",
          $"UserId: {userId}, DeletedBy: {deletedBy}",
          _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to delete user", ex, _logger.GetCorrelationId());
        throw;
      }
    }

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

    /// <summary>
    /// Map ApplicationUser to AdminUserDto with hydrated roles and permissions
    /// </summary>
    private async Task<AdminUserDto> MapUserToAdminUserDtoAsync(ApplicationUser user)
    {
      var unitRoles = await _dbContext.UserUnitRoles
        .Where(ur => ur.UserId == user.Id)
        .Include(ur => ur.Role)
        .Include(ur => ur.Unit)
        .Select(ur => new UserUnitRoleDto(
          UserId: ur.UserId,
          RoleId: ur.RoleId,
          RoleName: ur.Role.Name ?? "",
          UnitId: ur.UnitId,
          UnitName: ur.Unit.Name ?? "",
          ValidFrom: ur.ValidFrom,
          ValidTo: ur.ValidTo,
          IsActive: ur.ValidFrom == null || ur.ValidFrom <= DateTime.UtcNow &&
                    (ur.ValidTo == null || ur.ValidTo >= DateTime.UtcNow)
        ))
        .ToListAsync();

      var directPermissions = await _dbContext.UserDirectPermissions
        .Where(udp => udp.UserId == user.Id)
        .Include(udp => udp.Permission)
        .Select(udp => new DirectPermissionDto(
          UserId: udp.UserId,
          PermissionId: udp.PermissionId,
          Module: udp.Permission.Module,
          Resource: udp.Permission.Resource,
          Action: udp.Permission.Action,
          IsGranted: udp.IsGranted,
          GrantedBy: udp.GrantedBy,
          ExpiresAt: udp.ExpiresAt
        ))
        .ToListAsync();

      return new AdminUserDto(
        Id: user.Id,
        FirstName: user.FirstName ?? "",
        LastName: user.LastName ?? "",
        Email: user.Email ?? "",
        Phone: user.PhoneNumber,
        Department: user.Department,
        BaseUnitId: user.BaseUnitId,
        BaseUnitName: user.BaseUnit?.Name,
        Status: user.Status ?? "active",
        JoinedDate: user.CreatedAt,
        LastLogin: user.LastLogin,
        AvatarUrl: null,
        UnitRoles: unitRoles,
        DirectPermissions: directPermissions
      );
    }
  }
}

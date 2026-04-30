using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.Rbac.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.Repositories
{
  /// <summary>
  /// Read repository for querying users with filters and hydrated data.
  /// Supports searching, pagination, and loading related entities.
  /// </summary>
  public class UserReadRepository : IUserReadRepository
  {
    private readonly CalciferAppDbContext _dbContext;

    public UserReadRepository(CalciferAppDbContext dbContext)
    {
      _dbContext = dbContext;
    }

    public async Task<List<AdminUserDto>> GetAllUsersAsync()
    {
      var users = await _dbContext.Users
        .Where(u => !u.IsDeleted)
        .Include(u => u.BaseUnit)
        .Include(u => u.Status)
        .Include(u => u.UnitRoles)
          .ThenInclude(ur => ur.Role)
        .Include(u => u.UnitRoles)
          .ThenInclude(ur => ur.Unit)
        .Include(u => u.DirectPermissions)
          .ThenInclude(udp => udp.Permission)
        .ToListAsync();

      return users.Select(u => MapToAdminUserDto(u)).ToList();
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(string userId)
    {
      var user = await _dbContext.Users
        .Where(u => u.Id == userId && !u.IsDeleted)
        .Include(u => u.BaseUnit)
        .Include(u => u.Status)
        .Include(u => u.UnitRoles)
          .ThenInclude(ur => ur.Role)
        .Include(u => u.UnitRoles)
          .ThenInclude(ur => ur.Unit)
        .Include(u => u.DirectPermissions)
          .ThenInclude(udp => udp.Permission)
        .FirstOrDefaultAsync();

      return user == null ? null : MapToAdminUserDto(user);
    }

    public async Task<int> GetActiveUsersCountAsync()
    {
      return await _dbContext.Users
        .Where(u => !u.IsDeleted && u.StatusId == 1)
        .CountAsync();
    }

    public async Task<int> GetTotalUsersCountAsync()
    {
      return await _dbContext.Users
        .Where(u => !u.IsDeleted)
        .CountAsync();
    }

    public async Task<PaginatedResponse<AdminUserDto>> SearchUsersAsync(string? search, int page = 1, int pageSize = 20)
    {
      var query = _dbContext.Users
        .Where(u => !u.IsDeleted)
        .Include(u => u.BaseUnit)
        .Include(u => u.Status)
        .Include(u => u.UnitRoles)
          .ThenInclude(ur => ur.Role)
        .Include(u => u.UnitRoles)
          .ThenInclude(ur => ur.Unit)
        .Include(u => u.DirectPermissions)
          .ThenInclude(udp => udp.Permission)
        .AsQueryable();

      // Apply search filter
      if (!string.IsNullOrEmpty(search))
      {
        var searchLower = search.ToLower();
        query = query.Where(u =>
          u.FirstName.ToLower().Contains(searchLower) ||
          u.LastName.ToLower().Contains(searchLower) ||
          (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
          (u.Department != null && u.Department.ToLower().Contains(searchLower))
        );
      }

      // Get total count
      var totalCount = await query.CountAsync();

      // Apply pagination
      var items = await query
        .OrderBy(u => u.FirstName)
        .ThenBy(u => u.LastName)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

      var userDtos = items.Select(u => MapToAdminUserDto(u)).ToList();

      return new PaginatedResponse<AdminUserDto>(
        Items: userDtos,
        TotalCount: totalCount,
        Page: page,
        PageSize: pageSize,
        TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
      );
    }

    /// <summary>
    /// Map ApplicationUser to AdminUserDto
    /// </summary>
    private AdminUserDto MapToAdminUserDto(ApplicationUser user)
    {
      var unitRoles = user.UnitRoles?
        .Where(ur => !ur.IsDeleted &&
               (ur.ValidFrom == null || ur.ValidFrom <= DateTime.UtcNow) &&
               (ur.ValidTo == null || ur.ValidTo >= DateTime.UtcNow))
        .Select(ur => new UserUnitRoleDto(
          UserId: ur.UserId,
          RoleId: ur.RoleId,
          RoleName: ur.Role?.Name ?? "",
          UnitId: ur.UnitId,
          UnitName: ur.Unit?.Name ?? "",
          ValidFrom: ur.ValidFrom,
          ValidTo: ur.ValidTo,
          IsActive: (ur.ValidFrom == null || ur.ValidFrom <= DateTime.UtcNow) &&
                (ur.ValidTo == null || ur.ValidTo >= DateTime.UtcNow)
        ))
        .ToList() ?? new List<UserUnitRoleDto>();

      var directPermissions = user.DirectPermissions?
        .Where(udp => !udp.IsDeleted &&
               (udp.ExpiresAt == null || udp.ExpiresAt >= DateTime.UtcNow))
        .Select(udp => new DirectPermissionDto(
          UserId: udp.UserId,
          PermissionId: udp.PermissionId,
          Module: udp.Permission?.Module ?? "",
          Resource: udp.Permission?.Resource ?? "",
          Action: udp.Permission?.Action ?? "",
          IsGranted: udp.IsGranted,
          GrantedBy: udp.GrantedBy,
          ExpiresAt: udp.ExpiresAt
        ))
        .ToList() ?? new List<DirectPermissionDto>();

      // Resolve status name from CommonStatus FK
      var statusName = user.Status?.StatusName?.ToLower() ?? "active";

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

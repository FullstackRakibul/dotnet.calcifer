using Calcifer.Api.Rbac.DTOs;

namespace Calcifer.Api.Rbac.Repositories
{
  /// <summary>
  /// Read repository for querying users with filters and hydrated data.
  /// Used by IUserAdminService for admin panel listings.
  /// </summary>
  public interface IUserReadRepository
  {
    /// <summary>Get all users with their roles and direct permissions hydrated</summary>
    Task<List<AdminUserDto>> GetAllUsersAsync();

    /// <summary>Get specific user by ID with full details</summary>
    Task<AdminUserDto?> GetUserByIdAsync(string userId);

    /// <summary>Get active users count</summary>
    Task<int> GetActiveUsersCountAsync();

    /// <summary>Get total users count</summary>
    Task<int> GetTotalUsersCountAsync();

    /// <summary>Search users by email or name with pagination</summary>
    Task<PaginatedResponse<AdminUserDto>> SearchUsersAsync(string? search, int page, int pageSize);
  }

  /// <summary>
  /// Read repository for querying audit logs with filtering and pagination.
  /// Used by IAuditLogService for admin audit log listings.
  /// </summary>
  public interface IAuditLogRepository
  {
    /// <summary>Get paginated audit logs with filters applied</summary>
    Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter, int page, int pageSize);

    /// <summary>Get audit log count for the last 7 days</summary>
    Task<int> GetCountLast7DaysAsync();

    /// <summary>Get all audit logs for export (no pagination)</summary>
    Task<List<AuditLogDto>> GetAllAuditLogsAsync(AuditLogFilter filter);
  }
}

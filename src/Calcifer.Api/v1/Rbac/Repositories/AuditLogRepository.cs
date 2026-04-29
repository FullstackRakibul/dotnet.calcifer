using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.Licensing;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Entities;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.Repositories
{
  /// <summary>
  /// Read repository for querying audit logs with filtering and pagination.
  /// Supports complex filter scenarios and bulk export operations.
  /// </summary>
  public class AuditLogRepository : IAuditLogRepository
  {
    private readonly CalciferAppDbContext _dbContext;

    public AuditLogRepository(CalciferAppDbContext dbContext)
    {
      _dbContext = dbContext;
    }

    public async Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter, int page = 1, int pageSize = 20)
    {
      var query = ApplyFilters(_dbContext.AuditLogs.AsQueryable(), filter);

      // Get total count before pagination
      var totalCount = await query.CountAsync();

      // Apply pagination
      var items = await query
        .OrderByDescending(al => al.Timestamp)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(al => MapToDto(al))
        .ToListAsync();

      return new PaginatedResponse<AuditLogDto>(
        Items: items,
        TotalCount: totalCount,
        Page: page,
        PageSize: pageSize,
        TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
      );
    }

    public async Task<int> GetCountLast7DaysAsync()
    {
      var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
      return await _dbContext.AuditLogs
        .Where(al => al.Timestamp >= sevenDaysAgo)
        .CountAsync();
    }

    public async Task<List<AuditLogDto>> GetAllAuditLogsAsync(AuditLogFilter filter)
    {
      var query = ApplyFilters(_dbContext.AuditLogs.AsQueryable(), filter);

      var items = await query
        .OrderByDescending(al => al.Timestamp)
        .Select(al => MapToDto(al))
        .ToListAsync();

      return items;
    }

    /// <summary>
    /// Apply filters to audit log query
    /// </summary>
    private IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogFilter filter)
    {
      if (!string.IsNullOrEmpty(filter.Search))
      {
        var searchLower = filter.Search.ToLower();
        query = query.Where(al =>
          al.UserName.ToLower().Contains(searchLower) ||
          al.UserEmail.ToLower().Contains(searchLower) ||
          al.Action.ToLower().Contains(searchLower) ||
          al.Details.ToLower().Contains(searchLower)
        );
      }

      if (!string.IsNullOrEmpty(filter.Module))
        query = query.Where(al => al.Module == filter.Module);

      if (!string.IsNullOrEmpty(filter.Action))
        query = query.Where(al => al.Action == filter.Action);

      if (!string.IsNullOrEmpty(filter.Status))
        query = query.Where(al => al.Status == filter.Status);

      if (!string.IsNullOrEmpty(filter.UserId))
        query = query.Where(al => al.UserId == filter.UserId);

      if (filter.FromDate.HasValue)
        query = query.Where(al => al.Timestamp >= filter.FromDate);

      if (filter.ToDate.HasValue)
        query = query.Where(al => al.Timestamp <= filter.ToDate);

      return query;
    }

    /// <summary>
    /// Map AuditLog entity to DTO
    /// </summary>
    private AuditLogDto MapToDto(AuditLog auditLog)
    {
      return new AuditLogDto(
        Id: auditLog.Id.ToString(),
        Timestamp: auditLog.Timestamp,
        UserId: auditLog.UserId,
        UserName: auditLog.UserName ?? "",
        UserEmail: auditLog.UserEmail ?? "",
        Action: auditLog.Action ?? "",
        Module: auditLog.Module ?? "",
        Resource: auditLog.Resource ?? "",
        ResourceId: auditLog.ResourceId,
        Details: auditLog.Details ?? "",
        Status: auditLog.Status ?? "unknown",
        IpAddress: auditLog.IpAddress ?? "",
        Location: null,
        OldValue: auditLog.OldValue,
        NewValue: auditLog.NewValue
      );
    }
  }
}

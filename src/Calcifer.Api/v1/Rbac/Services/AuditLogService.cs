using Calcifer.Api.DbContexts;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Interfaces;
using Calcifer.Api.Rbac.Repositories;
using System.Text;

namespace Calcifer.Api.Rbac.Services
{
  /// <summary>
  /// Service for querying and managing audit logs.
  /// Handles filtered searches, pagination, and exports (CSV/Excel).
  /// </summary>
  public class AuditLogService : IAuditLogService
  {
    private readonly CalciferAppDbContext _dbContext;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogWriter _logger;

    public AuditLogService(
      CalciferAppDbContext dbContext,
      IAuditLogRepository auditLogRepository,
      ILogWriter logger)
    {
      _dbContext = dbContext;
      _auditLogRepository = auditLogRepository;
      _logger = logger;
    }

    public async Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get audit logs",
          "AuditLog",
          $"Page: {page}, PageSize: {pageSize}, Filter: {filter.Module ?? "all"}",
          _logger.GetCorrelationId());

        return await _auditLogRepository.GetAuditLogsAsync(filter, page, pageSize);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get audit logs", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<int> GetCountLast7DaysAsync()
    {
      try
      {
        return await _auditLogRepository.GetCountLast7DaysAsync();
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get audit log count for last 7 days", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<byte[]> ExportAuditLogsAsync(AuditLogFilter filter, string format = "csv", CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Export audit logs",
          "AuditLog",
          $"Format: {format}, Filter: {filter.Module ?? "all"}",
          _logger.GetCorrelationId());

        var logs = await _auditLogRepository.GetAllAuditLogsAsync(filter);

        if (format.ToLower() == "csv")
        {
          return ExportToCsv(logs);
        }
        else if (format.ToLower() == "xlsx")
        {
          return await ExportToExcelAsync(logs);
        }
        else
        {
          throw new Exception("Unsupported export format. Use 'csv' or 'xlsx'");
        }
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to export audit logs", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    /// <summary>
    /// Export audit logs to CSV format
    /// </summary>
    private byte[] ExportToCsv(List<AuditLogDto> logs)
    {
      var sb = new StringBuilder();

      // CSV Header
      sb.AppendLine("Id,Timestamp,UserId,UserName,UserEmail,Action,Module,Resource,ResourceId,Details,Status,IpAddress,Location,OldValue,NewValue");

      // CSV Data rows
      foreach (var log in logs)
      {
        sb.AppendLine($"\"{log.Id}\",\"{log.Timestamp}\",\"{log.UserId}\",\"{log.UserName}\",\"{log.UserEmail}\"," +
          $"\"{log.Action}\",\"{log.Module}\",\"{log.Resource}\",\"{log.ResourceId}\",\"{EscapeCsv(log.Details)}\"," +
          $"\"{log.Status}\",\"{log.IpAddress}\",\"{log.Location}\",\"{EscapeCsv(log.OldValue)}\",\"{EscapeCsv(log.NewValue)}\"");
      }

      return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Export audit logs to Excel format (XLSX)
    /// Note: Requires EPPlus or ClosedXML NuGet package
    /// For now, we'll convert to CSV as fallback
    /// </summary>
    private async Task<byte[]> ExportToExcelAsync(List<AuditLogDto> logs)
    {
      // Placeholder: Implement with EPPlus library
      // For now, return CSV as fallback
      await _logger.LogValidationAsync(
        "Export to Excel",
        "Not implemented - using CSV fallback",
        "EPPlus package needed for XLSX export",
        _logger.GetCorrelationId());

      return ExportToCsv(logs);
    }

    /// <summary>
    /// Escape special characters in CSV values
    /// </summary>
    private string EscapeCsv(string? value)
    {
      if (string.IsNullOrEmpty(value))
        return "";

      return value.Replace("\"", "\"\"");
    }
  }
}

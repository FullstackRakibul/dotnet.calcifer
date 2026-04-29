using Calcifer.Api.Rbac.DTOs;

namespace Calcifer.Api.Rbac.Interfaces
{
	public interface IAuditLogService
	{
		Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(
			AuditLogFilter filter, int page, int pageSize, CancellationToken ct = default);

		Task<byte[]> ExportAuditLogsAsync(AuditLogFilter filter, string format = "csv", CancellationToken ct = default);

		/// <summary>Get count of audit events in the last 7 days (for overview dashboard)</summary>
		Task<int> GetCountLast7DaysAsync();
	}
}

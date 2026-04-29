namespace Calcifer.Api.Rbac.DTOs
{
	public record AdminOverviewStats(
	int ActiveUsers,
	int TotalUsers,
	int RolesCount,
	int AuditEventsLast7Days,
	int ActiveSessionsCount,
	string SystemUptime
);
}

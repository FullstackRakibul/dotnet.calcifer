namespace Calcifer.Api.Rbac.DTOs
{
	public record AuditLogDto(
	string Id, DateTime Timestamp,
	string UserId, string UserName, string UserEmail,
	string Action, string Module, string Resource,
	string? ResourceId, string Details,
	string Status,
	string IpAddress, string? Location,
	string? OldValue, string? NewValue
);
}

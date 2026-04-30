namespace Calcifer.Api.Rbac.DTOs
{
	public record OrgUnitDto(
	int Id, string Code, string Name,
	string? Description, int? ParentId,
	bool IsActive, string Level,
	int MembersCount,
	DateTime CreatedDate, DateTime LastModified,
	List<OrgUnitDto>? Children = null
);
}

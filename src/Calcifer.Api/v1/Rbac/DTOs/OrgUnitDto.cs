namespace Calcifer.Api.Rbac.DTOs
{
	public record OrgUnitDto(
	int Id, string Code, string Name,
	string? Description, int Level, int? ParentId,
	List<OrgUnitDto>? Children
);
}

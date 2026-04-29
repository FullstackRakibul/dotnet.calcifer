using Calcifer.Api.Rbac.DTOs;

namespace Calcifer.Api.Rbac.Interfaces
{
	public interface IOrganizationUnitService
	{
		Task<List<OrgUnitDto>> GetAllUnitsAsync(CancellationToken ct = default);
		Task<List<OrgUnitDto>> GetUnitTreeAsync(CancellationToken ct = default);
		Task<OrgUnitDto?> GetUnitByIdAsync(int id, CancellationToken ct = default);
		Task<OrgUnitDto> CreateUnitAsync(CreateOrgUnitRequest request, string createdBy, CancellationToken ct = default);
		Task<OrgUnitDto> UpdateUnitAsync(int id, UpdateOrgUnitRequest request, string updatedBy, CancellationToken ct = default);
		Task<bool> DeleteUnitAsync(int id, string deletedBy, CancellationToken ct = default);
	}
}

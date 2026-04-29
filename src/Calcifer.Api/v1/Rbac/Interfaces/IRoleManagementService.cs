// ============================================================
//  IRbacService.cs
//
//  Isolation rule: this interface is the ONLY surface the rest
//  of the application depends on. Controllers, filters, and
//  minimal APIs import this interface — never RbacService directly.
//
//  13 methods in 5 categories:
//    Resolution (3) · Cache (2) · Roles (3) · Direct (2) · RoleConfig (3)
// ============================================================

using Calcifer.Api.Rbac.DTOs;

namespace Calcifer.Api.Rbac.Interfaces
{
	public interface IRoleManagementService
	{
		Task<List<RoleDto>> GetAllRolesAsync(CancellationToken ct = default);
		Task<RoleDto?> GetRoleByIdAsync(string id, CancellationToken ct = default);
		Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, string createdBy, CancellationToken ct = default);
		Task<RoleDto> UpdateRoleAsync(string id, UpdateRoleRequest request, string updatedBy, CancellationToken ct = default);
		Task<bool> DeleteRoleAsync(string id, string deletedBy, CancellationToken ct = default);

		// Bulk sync permissions for a role
		Task SyncRolePermissionsAsync(string roleId, int[] permissionIds, string updatedBy, CancellationToken ct = default);
	}
}
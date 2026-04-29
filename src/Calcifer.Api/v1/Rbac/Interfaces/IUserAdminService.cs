using Calcifer.Api.Rbac.DTOs;

namespace Calcifer.Api.Rbac.Interfaces
{
	public interface IUserAdminService
	{
		Task<List<AdminUserDto>> GetAllUsersAsync(CancellationToken ct = default);
		Task<AdminUserDto?> GetUserByIdAsync(string id, CancellationToken ct = default);
		Task<AdminUserDto> CreateUserAsync(CreateUserRequest request, string createdBy, CancellationToken ct = default);
		Task<AdminUserDto> UpdateUserAsync(string id, UpdateUserRequest request, string updatedBy, CancellationToken ct = default);
		Task<bool> DeleteUserAsync(string id, string deletedBy, CancellationToken ct = default);

		// ── Stats methods for overview dashboard ────────────────────
		Task<int> GetActiveUsersCountAsync();
		Task<int> GetTotalUsersCountAsync();

		// ── Search with pagination ──────────────────────────────────
		Task<PaginatedResponse<AdminUserDto>> SearchUsersAsync(string? search, int page = 1, int pageSize = 20);

		// ── User unit roles management ──────────────────────────────
		Task<List<UserUnitRoleDto>> GetUserUnitRolesAsync(string userId, CancellationToken ct = default);
		Task<UserUnitRoleDto> AssignUnitRoleAsync(string userId, AssignUnitRoleRequest request, string assignedBy, CancellationToken ct = default);
		Task<bool> RevokeUnitRoleAsync(string userId, RevokeUnitRoleRequest request, string revokedBy, CancellationToken ct = default);

		// ── User direct permissions management ──────────────────────
		Task<List<DirectPermissionDto>> GetUserDirectPermissionsAsync(string userId, CancellationToken ct = default);
		Task<DirectPermissionDto> GrantDirectPermissionAsync(string userId, SetDirectPermissionRequest request, string grantedBy, CancellationToken ct = default);
		Task<bool> RevokeDirectPermissionAsync(string userId, int permissionId, string revokedBy, CancellationToken ct = default);

		// ── Effective permissions summary ────────────────────────────
		Task<UserPermissionSummary> GetUserEffectivePermissionsAsync(string userId, CancellationToken ct = default);
	}
}

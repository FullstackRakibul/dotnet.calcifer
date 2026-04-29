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
	}
}

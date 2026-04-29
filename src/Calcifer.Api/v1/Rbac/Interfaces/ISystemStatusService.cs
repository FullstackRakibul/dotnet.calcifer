using Calcifer.Api.Rbac.DTOs;
namespace Calcifer.Api.Rbac.Interfaces
{
	public interface ISystemStatusService
	{
		Task<SystemHealthDto> GetSystemStatusAsync(CancellationToken ct = default);
	}
}

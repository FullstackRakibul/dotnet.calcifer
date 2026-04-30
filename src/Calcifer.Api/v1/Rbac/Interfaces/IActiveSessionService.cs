using Calcifer.Api.Rbac.DTOs;

namespace Calcifer.Api.Rbac.Interfaces
{
	public interface IActiveSessionService
	{
		Task<List<ActiveSessionDto>> GetActiveSessionsAsync();
		Task<bool> RevokeSessionAsync(string sessionId, string revokedBy);
		Task<int> RevokeAllSessionsExceptCurrentAsync(string currentSessionId, string revokedBy);
	}
}

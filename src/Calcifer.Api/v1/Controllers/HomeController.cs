using Microsoft.AspNetCore.Mvc;
using Calcifer.Api.Helper.ApiResponse;

namespace Calcifer.Api.Controllers
{
	/// <summary>
	/// Welcome controller — the ONLY controller kept as a reference example.
	/// All real endpoints use Minimal APIs in AuthHandler/MinimalApis/ and Rbac/MinimalApis/.
	/// </summary>
	[Route("api/v1")]
	[ApiController]
	public class HomeController : ControllerBase
	{
		/// <summary>
		/// Public welcome endpoint — no authentication required.
		/// Returns a greeting with API metadata.
		/// </summary>
		[HttpGet("welcome")]
		[HttpGet("")]
		public IActionResult Welcome()
		{
			return Ok(new ApiResponseDto<object>
			{
				Status = true,
				Message = "Welcome to Calcifer Cathedra 🏛️",
				Data = new
				{
					Name = "Calcifer.Api",
					Version = "1.1.0",
					Framework = ".NET 8.0",
					Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
					Timestamp = DateTime.UtcNow,
					Documentation = "/swagger"
				}
			});
		}
	}
}

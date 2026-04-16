using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Calcifer.Api.DTOs.AuthDTO;
using Calcifer.Api.Services.AuthService;

namespace Calcifer.Api.Controllers.AuthController
{
	/// <summary>
	/// Manages ASP.NET Identity roles (create, assign to user).
	/// For RBAC permission management use /api/v1/rbac/* endpoints.
	/// Restricted to SUPERADMIN.
	/// </summary>
	[Route("api/v1/[controller]")]
	[ApiController]
	[Authorize(Roles = "SUPERADMIN")]
	public class RoleController : ControllerBase
	{
		private readonly RoleService _roleService;

		public RoleController(RoleService roleService)
			=> _roleService = roleService;

		// POST api/v1/role/create
		[HttpPost("create")]
		public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto dto)
		{
			var (success, message, _) = await _roleService.CreateRoleAsync(dto);

			return success
				? Ok(new { status = true, message })
				: BadRequest(new { status = false, message });
		}

		// POST api/v1/role/assign
		[HttpPost("assign")]
		public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto dto)
		{
			var (success, message) = await _roleService.AssignRoleAsync(dto);

			return success
				? Ok(new { status = true, message })
				: BadRequest(new { status = false, message });
		}
	}
}
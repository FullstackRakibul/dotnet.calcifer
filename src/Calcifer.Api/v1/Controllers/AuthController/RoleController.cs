using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Calcifer.Api.DTOs.AuthDTO;
using Calcifer.Api.Services.AuthService;

namespace Calcifer.Api.Controllers.AuthController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMINISTRATOR")]
    public class RoleController : ControllerBase
    {
        private readonly RoleService _roleService;

        public RoleController(RoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto request)
        {
            var result = await _roleService.CreateRoleAsync(request.RoleName, request.Description);
            return result ? Ok("Role created successfully") : BadRequest("Role already exists or failed to create");
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto request)
        {
            var result = await _roleService.AssignRoleAsync(request.UserId, request.RoleName);
            return result ? Ok("Role assigned successfully") : BadRequest("Failed to assign role");
        }
    }

}

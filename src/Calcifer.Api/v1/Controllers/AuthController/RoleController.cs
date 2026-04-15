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
            var (success, message, _) = await _roleService.CreateRoleAsync(request);
            return success ? Ok(new { message }) : BadRequest(new { message });
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto request)
        {
            var (success, message) = await _roleService.AssignRoleAsync(request);
            return success ? Ok(new { message }) : BadRequest(new { message });
        }
    }

}

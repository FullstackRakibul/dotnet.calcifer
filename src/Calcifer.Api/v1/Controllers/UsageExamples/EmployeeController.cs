using Calcifer.Api.AuthHandler.Filters;
using Calcifer.Api.Interface.UsageExamples;
using Calcifer.Api.Rbac.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Calcifer.Api.Controllers.UsageExamples
{
	[Route("api/hcm/employees")]
	[ApiController]
	public class EmployeeController : ControllerBase
	{
		private readonly IEmployeeService _service;
		private readonly IRoleManagementService _rbac;

		public EmployeeController(IEmployeeService service, IRoleManagementService rbac)
		{
			_service = service;
			_rbac = rbac;
		}

		// Any authenticated user can attempt this route;
		// RbacAuthorizationFilter checks the [RequirePermission] attribute.
		[HttpGet]
		[RequirePermission("HCM", "Employee", "Read")]
		public async Task<IActionResult> GetAll()
		{
			// Employee role can only see themselves (self-scope)
			if (User.IsInRole("Employee"))
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
				var self = await _service.GetByUserIdAsync(userId);
				return Ok(self);
			}

			var all = await _service.GetAllAsync();
			return Ok(all);
		}

		[HttpPost]
		[RequirePermission("HCM", "Employee", "Create")]
		public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
		{
			var result = await _service.CreateAsync(dto);
			return CreatedAtAction(nameof(GetAll), result);
		}

		[HttpDelete("{id}")]
		[RequirePermission("HCM", "Employee", "Delete")]
		public async Task<IActionResult> Delete(int id)
		{
			await _service.DeleteAsync(id);
			return NoContent();
		}
	}
}

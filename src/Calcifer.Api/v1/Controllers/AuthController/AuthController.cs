using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DTOs.AuthDTO;
using Calcifer.Api.Services.AuthService;

namespace Calcifer.Api.Controllers.AuthController
{
	[Route("api/v1/Controllers/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly AuthService _authService;
		private readonly SignInManager<ApplicationUser> _signInManager;

		public AuthController(AuthService authService,
							  SignInManager<ApplicationUser> signInManager)
		{
			_authService = authService;
			_signInManager = signInManager;
		}

		// POST api/v1/auth/register
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
		{
			var (success, token, errors) = await _authService.RegisterAsync(dto);

			if (!success)
				return BadRequest(new { status = false, errors });

			return Ok(new { status = true, access_token = token });
		}

		// POST api/v1/auth/login
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
		{
			return Ok("this is a test ");
			var (success, token, errorMessage) = await _authService.LoginAsync(dto);

			return success
				? Ok(new { status = true, access_token = token })
				: Unauthorized(new { status = false, message = errorMessage });
		}

		// POST api/v1/auth/logout
		[HttpPost("logout")]
		[Authorize]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return Ok(new { status = true, message = "Logged out successfully." });
		}
	}
}
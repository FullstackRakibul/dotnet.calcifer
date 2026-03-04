using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DTOs.AuthDTO;
using Calcifer.Api.Services.AuthService;

namespace Calcifer.Api.Controllers.AuthController
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(AuthService authService, SignInManager<ApplicationUser> signInManager)
        {
            _authService = authService;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerDto)
        {
            var (success, access_token, errors) = await _authService.RegisterAsync(registerDto);
            if (!success)
                return BadRequest(new { errors });

            return Ok(new { access_token });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var (success, access_token, errorMessage) = await _authService.LoginAsync(request.Email, request.Password);
            return success ? Ok(new { access_token }) : Unauthorized(new { message = errorMessage });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("User logged out successfully");
        }
    }
}

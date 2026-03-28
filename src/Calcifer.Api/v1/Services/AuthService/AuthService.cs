using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Calcifer.Api.Services;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DTOs.AuthDTO;

namespace Calcifer.Api.Services.AuthService
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TokenService _tokenService;

        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, TokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        public async Task<(bool Success, string Token, string ErrorMessage)> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return (false, string.Empty, "User not found.");

            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
            if (!result.Succeeded) return (false, string.Empty, "Invalid credentials.");

            var token = await _tokenService.GenerateJwtToken(user);
            if (string.IsNullOrEmpty(token))
                return (false, string.Empty, "Token generation failed.");
            return (true, token, string.Empty);
        }


        // register new account
        public async Task<(bool Success, string Access_token, IEnumerable<string> Errors)> RegisterAsync(RegisterRequestDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return (false, string.Empty, new List<string> { "Email already in use." });
            }

            // Check for duplicate Employee ID
            var existingEmployee = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == dto.EmployeeId);
            if (existingEmployee != null)
            {
                return (false, string.Empty, new[] { "Employee ID is already taken." });
            }


            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber,
                EmployeeId = dto.EmployeeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = 1
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return (false, string.Empty, result.Errors.Select(e => e.Description));
            }

            // Optionally sign in or return JWT immediately
            var access_token = await _tokenService.GenerateJwtToken(user);
            return (true, access_token, null);
        }

    }
}

// ============================================================
//  AuthService.cs
//  Handles registration and login business logic.
//  Delegates token generation to TokenService.
//  Delegates role management to RoleService.
// ============================================================

using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Rbac.Enums;
using Calcifer.Api.DTOs.AuthDTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Services.AuthService
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        // ── Register ─────────────────────────────────────────────

        public async Task<(bool Success, string Message, UserProfileDto? Profile)> RegisterAsync(
            RegisterRequestDto dto, string? callerRole = null)
        {
            // Check for duplicate email
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return (false, "A user with this email already exists.", null);

            // Check for duplicate EmployeeId
            var existingEmp = _userManager.Users
                .FirstOrDefault(u => u.EmployeeId == dto.EmployeeId);
            if (existingEmp != null)
                return (false, $"Employee ID '{dto.EmployeeId}' is already registered.", null);

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmployeeId = dto.EmployeeId,
                Name = dto.Name,
                Region = dto.Region,
                StatusId = 1, // Active
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Registration failed for {Email}: {Errors}", dto.Email, errors);
                return (false, errors, null);
            }

            // Assign initial role — defaults to Employee if not specified
            // Guard: only SuperAdmin can assign privileged roles
            var roleToAssign = dto.InitialRole ?? DefaultRoles.Employee;
            var privilegedRoles = new[] { DefaultRoles.SuperAdmin, DefaultRoles.HrManager, DefaultRoles.ProductionManager, DefaultRoles.StoreManager };

            if (privilegedRoles.Contains(roleToAssign) && callerRole != DefaultRoles.SuperAdmin)
                roleToAssign = DefaultRoles.Employee;

            await _userManager.AddToRoleAsync(user, roleToAssign);

            _logger.LogInformation("User {Email} registered with role {Role}", dto.Email, roleToAssign);

            var profile = new UserProfileDto
            {
                Id = user.Id,
                EmployeeId = user.EmployeeId,
                Name = user.Name,
                Email = user.Email!,
                Region = user.Region,
                StatusId = user.StatusId,
                CreatedAt = user.CreatedAt,
                Roles = [roleToAssign]
            };

            return (true, "User registered successfully.", profile);
        }

        // ── Login ────────────────────────────────────────────────


        public async Task<(bool Success, string Message, LoginResponseDto? Response)> LoginAsync(LoginRequestDto dto)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(dto.Email) && string.IsNullOrWhiteSpace(dto.EmployeeId))
                    return (false, "Email or Employee ID is required.", null);

                if (string.IsNullOrWhiteSpace(dto.Password))
                    return (false, "Password is required.", null);

                ApplicationUser? user = null;

                // Find user
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    user = await _userManager.FindByEmailAsync(dto.Email);
                }
                else if (!string.IsNullOrWhiteSpace(dto.EmployeeId))
                {
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == dto.EmployeeId);
                }

                if (user == null)
                    return (false, "Invalid credentials.", null);

                // Account checks
                if (user.IsDeleted)
                    return (false, "This account has been deactivated. Contact your administrator.", null);

                if (user.StatusId != 1)
                    return (false, "This account is not active. Contact your administrator.", null);

                // Password check
                var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

                if (result.IsLockedOut)
                    return (false, "Account is locked after too many failed attempts. Try again later.", null);

                if (!result.Succeeded)
                    return (false, "Invalid credentials.", null);

                // Generate token
                var token = await _tokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("User {UserIdentifier} logged in successfully",
                    dto.Email ?? dto.EmployeeId);

                var response = new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email!,
                    EmployeeId = user.EmployeeId,
                    Roles = roles
                };

                return (true, "Login successful.", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for {UserIdentifier}",
                    dto.Email ?? dto.EmployeeId);

                return (false, "An unexpected error occurred. Please try again later.", null);
            }
        }

        //public async Task<(bool Success, string Message, LoginResponseDto? Response)> LoginAsync(
        //    LoginRequestDto dto)
        //{
        //    // Use IgnoreQueryFilters to allow logging in even if user is soft-deleted
        //    // (so we can return a meaningful error rather than "not found")
        //    var user = await _userManager.FindByEmailAsync(dto.Email);

        //    if (user == null)
        //        return (false, "Invalid email or password.", null);

        //    if (user.IsDeleted)
        //        return (false, "This account has been deactivated. Contact your administrator.", null);

        //    if (user.StatusId != 1) // not Active
        //        return (false, "This account is not active. Contact your administrator.", null);

        //    var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

        //    if (result.IsLockedOut)
        //        return (false, "Account is locked after too many failed attempts. Try again in 15 minutes.", null);

        //    if (!result.Succeeded)
        //        return (false, "Invalid email or password.", null);

        //    // Generate JWT with RBAC permission claims
        //    var token = await _tokenService.GenerateTokenAsync(user);
        //    var roles = await _userManager.GetRolesAsync(user);

        //    _logger.LogInformation("User {Email} logged in successfully", dto.Email);

        //    var response = new LoginResponseDto
        //    {
        //        Token = token,
        //        ExpiresAt = DateTime.UtcNow.AddHours(1), // matches JwtSettings.ExpirationInMinutes
        //        UserId = user.Id,
        //        Name = user.Name,
        //        Email = user.Email!,
        //        EmployeeId = user.EmployeeId,
        //        Roles = roles
        //    };

        //    return (true, "Login successful.", response);
        //}



        // ── Profile ───────────────────────────────────────────────

        public async Task<UserProfileDto?> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return new UserProfileDto
            {
                Id = user.Id,
                EmployeeId = user.EmployeeId,
                Name = user.Name,
                Email = user.Email!,
                Region = user.Region,
                StatusId = user.StatusId,
                CreatedAt = user.CreatedAt,
                Roles = roles
            };
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

            return (true, "Password changed successfully.");
        }
    }
}
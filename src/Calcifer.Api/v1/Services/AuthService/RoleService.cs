// ============================================================
//  RoleService.cs
//  Role management business logic.
//  Works with ASP.NET Identity's RoleManager.
//  Does NOT touch RBAC permission tables — that is
//  IRbacService's job.
// ============================================================

using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Rbac.Enums;
using Calcifer.Api.DTOs.AuthDTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Services.AuthService
{
    public class RoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger<RoleService> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        // ── Role CRUD ─────────────────────────────────────────────

        public async Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync()
        {
            return await _roleManager.Roles
                .Select(r => new RoleResponseDto
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, RoleResponseDto? Role)> CreateRoleAsync(
            CreateRoleRequestDto dto)
        {
            if (await _roleManager.RoleExistsAsync(dto.Name))
                return (false, $"Role '{dto.Name}' already exists.", null);

            var role = new ApplicationRole { Name = dto.Name, Description = dto.Description, IsSystemRole = false };
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);

            _logger.LogInformation("Role {RoleName} created", dto.Name);
            return (true, "Role created.", new RoleResponseDto { Id = role.Id, Name = role.Name!, Description = role.Description });
        }

        public async Task<bool> DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return false;

            // Protect system roles from deletion
            if (role.IsSystemRole)
                throw new InvalidOperationException($"System role '{role.Name}' cannot be deleted.");

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded;
        }

        // ── User ↔ Role assignment ────────────────────────────────

        public async Task<(bool Success, string Message)> AssignRoleAsync(AssignRoleRequestDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return (false, "User not found.");

            if (!await _roleManager.RoleExistsAsync(dto.RoleName))
                return (false, $"Role '{dto.RoleName}' does not exist.");

            if (await _userManager.IsInRoleAsync(user, dto.RoleName))
                return (true, $"User already has the role '{dto.RoleName}'.");

            var result = await _userManager.AddToRoleAsync(user, dto.RoleName);
            return result.Succeeded
                ? (true, $"Role '{dto.RoleName}' assigned.")
                : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<(bool Success, string Message)> RemoveRoleAsync(AssignRoleRequestDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return (false, "User not found.");

            // Never remove SuperAdmin from themselves if they're the last one
            if (dto.RoleName == DefaultRoles.SuperAdmin)
            {
                var superAdmins = await _userManager.GetUsersInRoleAsync(DefaultRoles.SuperAdmin);
                if (superAdmins.Count <= 1 && await _userManager.IsInRoleAsync(user, DefaultRoles.SuperAdmin))
                    return (false, "Cannot remove the last SuperAdmin.");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, dto.RoleName);
            return result.Succeeded
                ? (true, $"Role '{dto.RoleName}' removed.")
                : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return [];
            return await _userManager.GetRolesAsync(user);
        }
    }
}
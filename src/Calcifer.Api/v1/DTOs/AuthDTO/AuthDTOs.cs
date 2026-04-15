// ============================================================
//  AuthDTOs.cs
//  All Auth request / response DTOs in one file.
//  Matches the existing folder structure: DTOs/AuthDTO/
// ============================================================

namespace Calcifer.Api.DTOs.AuthDTO
{
    // ── Register ─────────────────────────────────────────────────

    public class RegisterRequestDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Region { get; set; }

        /// <summary>
        /// Optional: initial role to assign.
        /// If omitted, defaults to "Employee".
        /// Only SuperAdmin can assign higher roles at registration.
        /// </summary>
        public string? InitialRole { get; set; }
    }

    // ── Login ─────────────────────────────────────────────────────

    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public IEnumerable<string> Roles { get; set; } = [];

        /// <summary>Summary of unit:role assignments for UI use.</summary>
        public IEnumerable<string> UnitRoles { get; set; } = [];
    }

    // ── Role management ───────────────────────────────────────────

    public class CreateRoleRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class AssignRoleRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class RoleResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
    }

    // ── Password / profile ────────────────────────────────────────

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Region { get; set; }
        public int StatusId { get; set; }
        public DateTime CreatedAt { get; set; }
        public IEnumerable<string> Roles { get; set; } = [];
    }
}
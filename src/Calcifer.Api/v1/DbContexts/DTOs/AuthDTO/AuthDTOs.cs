// ============================================================
//  AuthDTOs.cs
//  Additional Auth request / response DTOs.
//  Only contains types NOT already defined in separate files:
//    - RegisterRequestDto  → RegisterRequestDto.cs
//    - LoginRequestDto     → LoginRequestDto.cs
//    - CreateRoleRequestDto → CreateRoleRequestDto.cs
//    - AssignRoleRequestDto → AssignRoleRequestDto.cs
// ============================================================

namespace Calcifer.Api.DbContexts.DTOs.AuthDTO
{
    // ── Login response ───────────────────────────────────────────

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
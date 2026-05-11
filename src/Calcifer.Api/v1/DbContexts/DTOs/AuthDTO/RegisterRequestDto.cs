using Microsoft.AspNetCore.Identity;

namespace Calcifer.Api.DbContexts.DTOs.AuthDTO
{
    public class RegisterRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Region { get; set; } = string.Empty;

        /// <summary>
        /// Optional: initial role to assign.
        /// If omitted, defaults to "Employee".
        /// Only SuperAdmin can assign higher roles at registration.
        /// </summary>
        public string? InitialRole { get; set; }
    }
}

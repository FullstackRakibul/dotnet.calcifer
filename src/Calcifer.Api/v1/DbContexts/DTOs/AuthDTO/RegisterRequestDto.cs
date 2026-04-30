using Microsoft.AspNetCore.Identity;

namespace Calcifer.Api.DbContexts.DTOs.AuthDTO
{
    public class RegisterRequestDto
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string EmployeeId { get; set; }
        public string? Region { get; set; }

        /// <summary>
        /// Optional: initial role to assign.
        /// If omitted, defaults to "Employee".
        /// Only SuperAdmin can assign higher roles at registration.
        /// </summary>
        public string? InitialRole { get; set; }
    }
}

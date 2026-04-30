namespace Calcifer.Api.DbContexts.DTOs.AuthDTO
{
    public class LoginRequestDto
    {
        public string? Email { get; set; }
        public string? EmployeeId { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}

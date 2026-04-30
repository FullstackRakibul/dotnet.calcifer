namespace Calcifer.Api.DbContexts.DTOs.AuthDTO
{
    public class CreateRoleRequestDto
    {
        public string RoleName { get; set; }

        /// <summary>Alias for RoleName — used by RoleService.</summary>
        public string Name
        {
            get => RoleName;
            set => RoleName = value;
        }

        public string? Description { get; set; }
    }
}

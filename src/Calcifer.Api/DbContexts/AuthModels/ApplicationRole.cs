using Microsoft.AspNetCore.Identity;

namespace Calcifer.Api.DbContexts.AuthModels
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public int? Status { get; set; }
    }
}

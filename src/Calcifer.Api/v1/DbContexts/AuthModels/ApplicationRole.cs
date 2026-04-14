using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Calcifer.Api.DbContexts.AuthModels
{
    public class ApplicationRole : IdentityRole
    {
		[MaxLength(250)]
		public string? Description { get; set; }

		/// <summary>
		/// FK → CommonStatus.Id (module = "RBAC")
		/// </summary>
		public int StatusId { get; set; } = 1; // Active by default

		public bool IsSystemRole { get; set; } = false; // seed roles are system roles

		// Navigation
		public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
	}
}

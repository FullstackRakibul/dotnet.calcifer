using Calcifer.Api.Rbac.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

		/// <summary>All unit-role assignments that reference this role (for UsersCount queries)</summary>
		public ICollection<UserUnitRole> Users { get; set; } = new List<UserUnitRole>();

		/// <summary>Shortcut: all permissions assigned to this role (for PermissionCount queries)</summary>
		[NotMapped]
		public IEnumerable<Permission> Permissions => RolePermissions.Select(rp => rp.Permission);
	}
}

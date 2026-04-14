// ============================================================
//  Permission.cs
//  One row = one atomic capability.
//  Example rows:
//      Module=HCM,      Resource=Employee,  Action=Create
//      Module=HCM,      Resource=Employee,  Action=Read
//      Module=Finance,  Resource=Payroll,   Action=Export
//
//  Seeded from RbacPermissionSeeder using the matrix in your
//  RBAC design document. Developers can add new rows at any
//  time — no code change needed in filters or services.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.Rbac.Entities
{
	[Table("Permissions")]
	public class Permission
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[MaxLength(50)]
		public string Module { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string Resource { get; set; } = string.Empty;

		[Required]
		[MaxLength(20)]
		public string Action { get; set; } = string.Empty;

		[MaxLength(250)]
		public string? Description { get; set; }

		public bool IsActive { get; set; } = true;

		// ── Navigation ───────────────────────────────────────────
		public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
		public ICollection<UserDirectPermission> DirectGrants { get; set; } = new List<UserDirectPermission>();

		// ── Convenience: canonical claim string ──────────────────
		[NotMapped]
		public string ClaimValue => $"{Module}:{Resource}:{Action}";
	}
}
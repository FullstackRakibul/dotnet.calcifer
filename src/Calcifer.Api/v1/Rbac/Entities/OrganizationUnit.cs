// ============================================================
//  OrganizationUnit.cs
//  Self-referencing tree. Supports: Company → Division →
//  Department → Team, or any depth you need.
//
//  A user can hold different roles at different units:
//      Ali = HR Manager @ Factory-1
//      Ali = Viewer     @ HQ
//  That mapping lives in UserUnitRole, not here.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Calcifer.Api.DbContexts.Common;
using Calcifer.Api.Rbac.Enums;

namespace Calcifer.Api.Rbac.Entities
{
	[Table("OrganizationUnits")]
	public class OrganizationUnit : AuditBase
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[MaxLength(50)]
		public string Code { get; set; } = string.Empty;

		[Required]
		[MaxLength(150)]
		public string Name { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Description { get; set; }

		public string Level { get; set; } = OrgUnitLevel.Department;

		public bool IsActive { get; set; } = true;

		// ── Self-referencing tree ────────────────────────────────
		public int? ParentId { get; set; }

		[ForeignKey("ParentId")]
		public OrganizationUnit? Parent { get; set; }

		public ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();

		// ── RBAC navigation ──────────────────────────────────────
		public ICollection<UserUnitRole> UserRoles { get; set; } = new List<UserUnitRole>();
	}
}
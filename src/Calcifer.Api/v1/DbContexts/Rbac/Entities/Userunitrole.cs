// ============================================================
//  UserUnitRole.cs
//  The multi-unit assignment table. This is the core of the
//  "Goal 2" requirement:
//      Ali = HR Manager  @ Factory-1   → row 1
//      Ali = Viewer      @ HQ          → row 2
//
//  Composite PK: (UserId, RoleId, UnitId)
//  This means a user can hold EXACTLY ONE role per unit.
//  If they need two roles at one unit, a composite "HR+Store"
//  role should be created instead.
//
//  ValidFrom / ValidTo allows time-bounded assignments:
//      temp contractors, cover duties, probation periods.
//  ValidTo = null means the assignment never expires.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Common;

namespace Calcifer.Api.DbContexts.Rbac.Entities
{
	[Table("UserUnitRoles")]
	public class UserUnitRole : AuditBase
	{
		// ── Composite PK (configured in OnModelCreating) ─────────
		[Required]
		public string UserId { get; set; } = string.Empty;

		[Required]
		public string RoleId { get; set; } = string.Empty;

		public int UnitId { get; set; }

		// ── Time-bounding ─────────────────────────────────────────
		public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
		public DateTime? ValidTo { get; set; }

		public bool IsActive { get; set; } = true;

		[MaxLength(500)]
		public string? Notes { get; set; }

		// ── Navigation ───────────────────────────────────────────
		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; } = null!;

		[ForeignKey("RoleId")]
		public ApplicationRole Role { get; set; } = null!;

		[ForeignKey("UnitId")]
		public OrganizationUnit Unit { get; set; } = null!;

		// ── Computed helper (not mapped) ─────────────────────────
		[NotMapped]
		public bool IsCurrentlyActive =>
			IsActive &&
			ValidFrom <= DateTime.UtcNow &&
			(ValidTo == null || ValidTo > DateTime.UtcNow);
	}
}
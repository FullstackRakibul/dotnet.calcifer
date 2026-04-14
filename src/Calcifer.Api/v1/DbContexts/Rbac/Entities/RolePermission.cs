// ============================================================
//  RolePermission.cs
//  Join table: ApplicationRole ↔ Permission
//  Composite PK: (RoleId, PermissionId)
//
//  "HR Manager has Create on HCM:Employee" →
//      RoleId = <HRManager guid>
//      PermissionId = <HCM:Employee:Create id>
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Calcifer.Api.DbContexts.AuthModels;

namespace Calcifer.Api.DbContexts.Rbac.Entities
{
	[Table("RolePermissions")]
	public class RolePermission
	{
		// ── Composite PK (configured in OnModelCreating) ─────────
		[Required]
		public string RoleId { get; set; } = string.Empty;

		public int PermissionId { get; set; }

		// ── Audit ────────────────────────────────────────────────
		public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
		public string? AssignedBy { get; set; }

		// ── Navigation ───────────────────────────────────────────
		[ForeignKey("RoleId")]
		public ApplicationRole Role { get; set; } = null!;

		[ForeignKey("PermissionId")]
		public Permission Permission { get; set; } = null!;
	}
}
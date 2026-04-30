// ============================================================
//  UserDirectPermission.cs
//  Direct permission override for a specific user.
//  Allows granting or denying a specific permission directly,
//  bypassing the role-based assignment.
//
//  Use cases:
//      - Emergency access: grant a permission temporarily
//      - Restriction: deny a specific capability for a user
//        even though their role normally allows it
//
//  Composite PK: (UserId, PermissionId)
//  IsGranted = true  → explicit grant (added to effective set)
//  IsGranted = false → explicit deny  (removed from effective set)
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.DbContexts.Common;

namespace Calcifer.Api.Rbac.Entities
{
	[Table("UserDirectPermissions")]
	public class UserDirectPermission : AuditBase
	{
		// ── Composite PK (configured in OnModelCreating) ─────────
		[Required]
		public string UserId { get; set; } = string.Empty;

		public int PermissionId { get; set; }

		// ── Grant or deny ────────────────────────────────────────
		/// <summary>
		/// true = explicitly granted (adds to effective permissions).
		/// false = explicitly denied (removes from effective permissions, overrides role grants).
		/// </summary>
		public bool IsGranted { get; set; } = true;

		/// <summary>Optional expiration. Null = never expires.</summary>
		public DateTime? ExpiresAt { get; set; }

		/// <summary>Human-readable reason for the override.</summary>
		[MaxLength(500)]
		public string? Reason { get; set; }

		/// <summary>UserId of the admin who granted/denied this.</summary>
		[MaxLength(450)]
		public string? GrantedBy { get; set; }

		// ── Status ───────────────────────────────────────────────
		public int StatusId { get; set; } = 1;

		// ── Navigation ───────────────────────────────────────────
		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; } = null!;

		[ForeignKey("PermissionId")]
		public Permission Permission { get; set; } = null!;
	}
}

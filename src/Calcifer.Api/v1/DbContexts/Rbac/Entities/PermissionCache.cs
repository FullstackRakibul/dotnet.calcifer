// ============================================================
//  PermissionCache.cs
//  Stores the resolved (flattened) permission set for each
//  user per organizational unit.
//
//  WHY this exists:
//  Computing permissions at request time requires 4 joins.
//  This table caches the result. The JWT "perms" claim is
//  built from it. Invalidated whenever:
//      - A UserUnitRole is added/removed/changed
//      - A UserDirectPermission is added/removed/changed
//      - A RolePermission is changed
//
//  The cache is keyed by (UserId, UnitId). If UnitId = null,
//  it represents the global (cross-unit) permission set.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Calcifer.Api.DbContexts.AuthModels;

namespace Calcifer.Api.DbContexts.Rbac.Entities
{
	[Table("PermissionCache")]
	public class PermissionCache
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		[Required]
		public string UserId { get; set; } = string.Empty;

		/// <summary>Null = global (all units merged).</summary>
		public int? UnitId { get; set; }

		[Required]
		public string PermissionSetJson { get; set; } = "[]";

		public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

		public DateTime? InvalidatedAt { get; set; }

		[NotMapped]
		public bool IsValid => InvalidatedAt == null;

		// ── Navigation ───────────────────────────────────────────
		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; } = null!;

		[ForeignKey("UnitId")]
		public OrganizationUnit? Unit { get; set; }
	}
}
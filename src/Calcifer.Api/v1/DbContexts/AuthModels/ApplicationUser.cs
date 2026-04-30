using Calcifer.Api.DbContexts.Common;
using Calcifer.Api.Rbac.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.AuthModels
{
    public class ApplicationUser : IdentityUser
    {
		// ── Business identity ────────────────────────────────────
		public Guid AppUserGuid { get; set; } = Guid.NewGuid();

		[Required]
		[MaxLength(50)]
		public string EmployeeId { get; set; } = string.Empty;

		[Required]
		[MaxLength(100)]
		public string Name { get; set; } = string.Empty;

		[MaxLength(100)]
		public string FirstName { get; set; } = string.Empty;

		[MaxLength(100)]
		public string LastName { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? Department { get; set; }

		[MaxLength(100)]
		public string? Region { get; set; }

		// ── Base organization unit ──────────────────────────────
		public int? BaseUnitId { get; set; }

		[ForeignKey("BaseUnitId")]
		public OrganizationUnit? BaseUnit { get; set; }

		// ── Login tracking ──────────────────────────────────────
		public DateTime? LastLogin { get; set; }

		// ── Status FK → CommonStatus ─────────────────────────────
		public int StatusId { get; set; }

		[ForeignKey("StatusId")]
		public CommonStatus? Status { get; set; }

		// ── Audit fields (flat — see class-level comment) ────────
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public string? CreatedBy { get; set; }

		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }

		/// <summary>Null until soft-deleted. Never DateTime.Now by default.</summary>
		public DateTime? DeletedAt { get; set; }
		public string? DeletedBy { get; set; }

		public bool IsDeleted { get; set; } = false;

		// ── RBAC navigation ──────────────────────────────────────
		public ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
		public ICollection<UserUnitRole> UnitRoles { get; set; } = new List<UserUnitRole>();
		public ICollection<UserDirectPermission> DirectPermissions { get; set; } = new List<UserDirectPermission>();
		public ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();
	}
}
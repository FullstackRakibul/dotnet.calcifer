using Calcifer.Api.DbContexts.Common;
using Calcifer.Api.DbContexts.Rbac.Entities;
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
		public string? Region { get; set; }

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
		public ICollection<UserUnitRole> UnitRoles { get; set; } = new List<UserUnitRole>();
		public ICollection<UserDirectPermission> DirectPermissions { get; set; } = new List<UserDirectPermission>();
	}
}
   
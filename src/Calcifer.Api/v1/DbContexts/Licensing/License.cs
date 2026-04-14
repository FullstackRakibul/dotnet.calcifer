// ============================================================
//  License.cs — CORRECTED
//
//  Changed from original:
//  - Was: License : TableOperationDetails (caused triple Id clash)
//  - Now: License : AuditBase  (clean single inheritance)
//
//  StatusId FK → CommonStatus is now a direct field,
//  not inherited through a broken chain.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Calcifer.Api.DbContexts.Common;

namespace Calcifer.Api.DbContexts.Licensing
{
	[Table("Licenses")]
	public class License : AuditBase
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		public Guid LicenseGuid { get; set; } = Guid.NewGuid();

		[Required]
		[MaxLength(250)]
		public string LicenseKey { get; set; } = string.Empty;

		[Required]
		[MaxLength(100)]
		public string OrganizationName { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? ContactEmail { get; set; }

		[Required]
		public int LicenseTypeId { get; set; }

		[Required]
		public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

		[Required]
		public DateTime ExpiresAt { get; set; }

		public int MaxUsers { get; set; } = 1;

		public bool IsActive { get; set; } = true;

		// ── Status FK ────────────────────────────────────────────
		public int StatusId { get; set; } = 1; // Active by default

		[ForeignKey("StatusId")]
		public CommonStatus? Status { get; set; }

		// ── Navigation ───────────────────────────────────────────
		[ForeignKey("LicenseTypeId")]
		public LicenseType LicenseType { get; set; } = null!;

		public ICollection<LicenseFeature> LicenseFeatures { get; set; } = new List<LicenseFeature>();
		public ICollection<LicenseActivation> Activations { get; set; } = new List<LicenseActivation>();

		// ── Computed helpers ─────────────────────────────────────
		[NotMapped]
		public bool IsExpired => ExpiresAt < DateTime.UtcNow;

		[NotMapped]
		public bool IsEffective => IsActive && !IsExpired && !IsDeleted;
	}
}
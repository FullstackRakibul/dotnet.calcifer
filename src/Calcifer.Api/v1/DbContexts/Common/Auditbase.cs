// ============================================================
//  AuditBase.cs
//  Foundation class — inherit this on every domain entity.

namespace Calcifer.Api.DbContexts.Common
{
	public abstract class AuditBase
	{
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public string? CreatedBy { get; set; }

		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }
		public DateTime? DeletedAt { get; set; }
		public string? DeletedBy { get; set; }
		public bool IsDeleted { get; set; } = false;
	}
}
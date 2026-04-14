using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.Common
{
	[Table("CommonStatus")]
	public class CommonStatus
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[MaxLength(50)]
		public string StatusName { get; set; } = string.Empty;

		[MaxLength(250)]
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// Which module/domain this status belongs to.
		/// Examples: "User", "License", "RBAC", "Production"
		/// Allows one table to serve all modules without collision.
		/// </summary>
		[Required]
		[MaxLength(50)]
		public string Module { get; set; } = string.Empty;

		public bool IsActive { get; set; } = true;

		public int SortOrder { get; set; }
	}
}

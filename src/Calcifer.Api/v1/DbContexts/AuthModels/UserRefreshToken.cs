using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.AuthModels
{
	/// <summary>
	/// Tracks JWT refresh tokens for active session management.
	/// Each row represents one user session (device/browser).
	/// Sessions are considered active when ExpiryTime > UtcNow.
	/// </summary>
	[Table("UserRefreshTokens")]
	public class UserRefreshToken
	{
		[Key]
		[MaxLength(450)]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		[Required]
		public string UserId { get; set; } = string.Empty;

		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; } = null!;

		/// <summary>The hashed refresh token value</summary>
		[Required]
		[MaxLength(512)]
		public string Token { get; set; } = string.Empty;

		/// <summary>When this token expires</summary>
		public DateTime ExpiryTime { get; set; }

		/// <summary>IP address of the client that created this session</summary>
		[MaxLength(45)]
		public string? IpAddress { get; set; }

		/// <summary>Device/browser identifier from User-Agent header</summary>
		[MaxLength(256)]
		public string? DeviceName { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
	}
}

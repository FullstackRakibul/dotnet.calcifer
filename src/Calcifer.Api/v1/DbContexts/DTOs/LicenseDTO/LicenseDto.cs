using System.ComponentModel.DataAnnotations;

namespace Calcifer.Api.DbContexts.DTOs.LicenseDTO
{
	public record LicenseResponseDto(
		int Id,
		Guid LicenseGuid,
		string LicenseKey,
		string OrganizationName,
		string? ContactEmail,
		int LicenseTypeId,
		string LicenseTypeName,
		DateTime IssuedAt,
		DateTime ExpiresAt,
		int MaxUsers,
		bool IsActive,
		bool IsExpired,
		bool IsEffective,
		int ActiveActivationCount,
		IEnumerable<LicenseFeatureDto> Features
	);

	public record LicenseFeatureDto(
		int Id,
		string FeatureCode,
		string? Description,
		bool IsEnabled
	);

	public record LicenseValidationResultDto
	{
		public bool IsValid { get; init; }
		public string? Message { get; init; }
		public string? OrganizationName { get; init; }
		public DateTime? ExpiresAt { get; init; }
		public IEnumerable<string>? EnabledFeatures { get; init; }
	}

	public record ValidateLicenseRequest(
		[Required, MaxLength(250)] string LicenseKey
	);

	public record CreateLicenseRequest(
		[Required, MaxLength(100)] string OrganizationName,
		[MaxLength(100)] string? ContactEmail,
		[Required] int LicenseTypeId,
		[Required] DateTime ExpiresAt,
		int MaxUsers = 50,
		List<string>? FeatureCodes = null
	);

	public record UpdateLicenseRequest(
		[MaxLength(100)] string? OrganizationName,
		[MaxLength(100)] string? ContactEmail,
		DateTime? ExpiresAt,
		int? MaxUsers,
		bool? IsActive
	);

	public record ActivateMachineRequest(
		[Required, MaxLength(250)] string LicenseKey,
		[Required, MaxLength(256)] string MachineId
	);

	public record SetFeatureRequest(
		[Required, MaxLength(50)] string FeatureCode,
		[Required] bool IsEnabled
	);
}
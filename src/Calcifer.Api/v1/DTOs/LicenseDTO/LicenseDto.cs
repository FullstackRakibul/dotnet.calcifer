using System.ComponentModel.DataAnnotations;

namespace Calcifer.Api.DTOs.LicenseDTO
{
	public record LicenseResponseDto(
		int Id,
		Guid LicenseGuid,
		string LicenseKey,
		string OrganizationName,
		string? ContactEmail,
		string LicenseTypeName,
		DateTime IssuedAt,
		DateTime ExpiresAt,
		int MaxUsers,
		bool IsActive,
		bool IsExpired,
		bool IsEffective,
		int ActiveActivations,
		List<LicenseFeatureDto> Features
	);

	public record LicenseFeatureDto(
		int Id,
		string FeatureCode,
		string? Description,
		bool IsEnabled
	);

	public record CreateLicenseRequest(
		[Required, MaxLength(100)] string OrganizationName,
		[MaxLength(100)] string? ContactEmail,
		[Required] int LicenseTypeId,
		[Required] DateTime ExpiresAt,
		int MaxUsers = 50
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
		[Required] bool IsEnabled
	);
}
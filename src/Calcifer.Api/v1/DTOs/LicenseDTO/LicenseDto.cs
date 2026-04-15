// ============================================================
//  LicenseDTOs.cs
//  All request/response DTOs for the licensing module.
//  Placed in DTOs/LicenseDTO/ to match the existing
//  project folder convention.
// ============================================================

namespace Calcifer.Api.DTOs.LicenseDTO
{
    // ── Outbound ─────────────────────────────────────────────────

    public class LicenseResponseDto
    {
        public int Id { get; set; }
        public Guid LicenseGuid { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string LicenseTypeName { get; set; } = string.Empty;
        public int LicenseTypeId { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int MaxUsers { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public int ActiveActivationCount { get; set; }
        public IEnumerable<LicenseFeatureDto> Features { get; set; } = [];
    }

    public class LicenseFeatureDto
    {
        public int Id { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class LicenseActivationDto
    {
        public int Id { get; set; }
        public string? MachineId { get; set; }
        public string? ActivatedByUserId { get; set; }
        public DateTime ActivatedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class LicenseValidationResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OrganizationName { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public IEnumerable<string> EnabledFeatures { get; set; } = [];
    }

    // ── Inbound ──────────────────────────────────────────────────

    public class CreateLicenseRequest
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public int LicenseTypeId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int MaxUsers { get; set; } = 1;

        /// <summary>
        /// Optional list of feature codes to enable at creation time.
        /// Example: ["HCM", "Production", "Inventory"]
        /// </summary>
        public IEnumerable<string> FeatureCodes { get; set; } = [];
    }

    public class UpdateLicenseRequest
    {
        public string? OrganizationName { get; set; }
        public string? ContactEmail { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsers { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ActivateMachineRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
    }

    public class SetFeatureRequest
    {
        public string FeatureCode { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public class ValidateLicenseRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
    }
}

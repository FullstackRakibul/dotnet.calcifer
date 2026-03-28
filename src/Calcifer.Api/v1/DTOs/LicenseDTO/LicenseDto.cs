namespace Calcifer.Api.DTOs.LicenseDTO
{
    // --- Requests ---

    public class CreateLicenseRequest
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public int LicenseTypeId { get; set; }
        public int MaxUsers { get; set; } = 1;
        public List<string> FeatureCodes { get; set; } = new();
    }

    public class ActivateLicenseRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string? MachineId { get; set; }
    }

    public class ValidateLicenseRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
    }

    // --- Responses ---

    public class LicenseResponse
    {
        public Guid LicenseGuid { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string LicenseTypeName { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int MaxUsers { get; set; }
        public int ActiveSeatCount { get; set; }
        public bool IsActive { get; set; }
        public List<string> Features { get; set; } = new();
    }

    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> EnabledFeatures { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
        public int? RemainingSeats { get; set; }
    }
}

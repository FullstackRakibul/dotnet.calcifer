using Calcifer.Api.DTOs.LicenseDTO;

namespace Calcifer.Api.Interface.Licensing
{
    public interface ILicenseService
    {
        Task<LicenseResponse> CreateLicenseAsync(CreateLicenseRequest request, string createdBy);
        Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey);
        Task<LicenseValidationResult> ActivateLicenseAsync(ActivateLicenseRequest request, string userId);
        Task<bool> DeactivateLicenseAsync(Guid licenseGuid, string deactivatedBy);
        Task<LicenseResponse?> GetLicenseByKeyAsync(string licenseKey);
        Task<LicenseResponse?> GetLicenseByIdAsync(int id);
        Task<List<LicenseResponse>> GetAllLicensesAsync();
        Task<bool> HasFeatureAsync(string licenseKey, string featureCode);
    }
}

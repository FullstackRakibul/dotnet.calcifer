// ============================================================
//  ILicenseService.cs
//  License engine contract.
//
//  Isolation rule: this interface lives entirely in the
//  licensing module. IRbacService never imports this and
//  this never imports IRbacService.
//
//  Responsibilities:
//    - Validate a license key (active, not expired, not revoked)
//    - Check whether a specific feature is enabled on a license
//    - Activate / deactivate a machine
//    - CRUD for admin management of license records
// ============================================================

using Calcifer.Api.DbContexts.DTOs.LicenseDTO;
using Calcifer.Api.DbContexts.Licensing;

namespace Calcifer.Api.Interface.Licensing
{
    public interface ILicenseService
    {
        // ── Validation (used by LicenseValidationFilter) ─────────

        /// <summary>
        /// Checks whether the current license is active, non-expired,
        /// and has the requested feature enabled.
        /// Called on every request by LicenseValidationFilter.
        /// </summary>
        Task<bool> IsFeatureEnabledAsync(string featureCode, CancellationToken ct = default);

        /// <summary>
        /// Returns the first active, non-expired license for validation.
        /// Returns null if no valid license exists.
        /// </summary>
        Task<License?> GetActiveLicenseAsync(CancellationToken ct = default);

        /// <summary>
        /// Validates a raw license key string.
        /// Returns the license record if valid, null if not found/expired/revoked.
        /// </summary>
        Task<License?> ValidateLicenseKeyAsync(string licenseKey, CancellationToken ct = default);

        // ── Machine activation ────────────────────────────────────

        /// <summary>
        /// Activates the current machine against a license.
        /// Enforces MaxUsers (max concurrent activations).
        /// </summary>
        Task<(bool Success, string Message)> ActivateMachineAsync(
            string licenseKey, string machineId,
            string? activatedByUserId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Deactivates a machine, freeing its activation slot.
        /// </summary>
        Task<bool> DeactivateMachineAsync(
            string licenseKey, string machineId,
            CancellationToken ct = default);

        /// <summary>
        /// Returns all active machine activations for a license.
        /// </summary>
        Task<IEnumerable<LicenseActivation>> GetActivationsAsync(
            int licenseId, CancellationToken ct = default);

        // ── Admin CRUD ────────────────────────────────────────────

        /// <summary>Returns all licenses (admin view).</summary>
        Task<IEnumerable<LicenseResponseDto>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Returns a single license by ID.</summary>
        Task<LicenseResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>Creates a new license record.</summary>
        Task<LicenseResponseDto> CreateAsync(CreateLicenseRequest request, CancellationToken ct = default);

        /// <summary>Updates an existing license.</summary>
        Task<bool> UpdateAsync(int id, UpdateLicenseRequest request, CancellationToken ct = default);

        /// <summary>Revokes a license (sets IsActive=false).</summary>
        Task<bool> RevokeAsync(int id, string revokedBy, CancellationToken ct = default);

        // ── Feature management ────────────────────────────────────

        /// <summary>Returns all features attached to a license.</summary>
        Task<IEnumerable<LicenseFeature>> GetFeaturesAsync(int licenseId, CancellationToken ct = default);

        /// <summary>Enables or disables a feature on a license.</summary>
        Task<bool> SetFeatureAsync(int licenseId, string featureCode, bool isEnabled, CancellationToken ct = default);
    }
}
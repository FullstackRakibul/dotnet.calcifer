// ============================================================
//  LicenseService.cs
//  Complete implementation of ILicenseService.
//
//  Design rules:
//  - Never imports IRbacService — fully isolated
//  - License key is generated as a cryptographic GUID string
//    so it cannot be guessed
//  - Machine activation enforces MaxUsers at the DB level
//    (unique index on LicenseId+MachineId) AND at service level
//  - IsFeatureEnabledAsync is the hot path called by the filter
//    on every request — it's kept as a single cheap query
// ============================================================

using System.Security.Cryptography;
using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.Licensing;
using Calcifer.Api.DTOs.LicenseDTO;
using Calcifer.Api.Interface.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Services.LicenseService
{
    public class LicenseService : ILicenseService
    {
        private readonly CalciferAppDbContext _db;
        private readonly ILogger<LicenseService> _logger;

        public LicenseService(CalciferAppDbContext db, ILogger<LicenseService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ── Validation (hot path) ────────────────────────────────

        public async Task<bool> IsFeatureEnabledAsync(
            string featureCode, CancellationToken ct = default)
        {
            // Single query: find an active, non-expired license that
            // has this feature enabled. Returns true/false.
            return await _db.LicenseFeatures
                .Include(f => f.License)
                .AnyAsync(f =>
                    f.FeatureCode == featureCode &&
                    f.IsEnabled &&
                    f.License.IsActive &&
                    !f.License.IsDeleted &&
                    f.License.ExpiresAt > DateTime.UtcNow,
                    ct);
        }

        public async Task<License?> GetActiveLicenseAsync(CancellationToken ct = default)
        {
            return await _db.Licenses
                .Include(l => l.LicenseType)
                .Include(l => l.LicenseFeatures)
                .Where(l =>
                    l.IsActive &&
                    !l.IsDeleted &&
                    l.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(l => l.IssuedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<License?> ValidateLicenseKeyAsync(
            string licenseKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(licenseKey)) return null;

            return await _db.Licenses
                .Include(l => l.LicenseType)
                .Include(l => l.LicenseFeatures)
                .Where(l =>
                    l.LicenseKey == licenseKey &&
                    l.IsActive &&
                    !l.IsDeleted &&
                    l.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync(ct);
        }

        // ── Machine activation ────────────────────────────────────

        public async Task<(bool Success, string Message)> ActivateMachineAsync(
            string licenseKey, string machineId,
            string? activatedByUserId = null,
            CancellationToken ct = default)
        {
            var license = await ValidateLicenseKeyAsync(licenseKey, ct);
            if (license == null)
                return (false, "License key is invalid, expired, or revoked.");

            // Check for existing activation on this machine
            var existing = await _db.LicenseActivations
                .FirstOrDefaultAsync(a =>
                    a.LicenseId == license.Id &&
                    a.MachineId == machineId &&
                    a.IsActive, ct);

            if (existing != null)
                return (true, "Machine is already activated on this license.");

            // Enforce MaxUsers — count current active activations
            var activeCount = await _db.LicenseActivations
                .CountAsync(a => a.LicenseId == license.Id && a.IsActive, ct);

            if (activeCount >= license.MaxUsers)
                return (false, $"License has reached the maximum of {license.MaxUsers} concurrent activations. Deactivate another machine first.");

            _db.LicenseActivations.Add(new LicenseActivation
            {
                LicenseId = license.Id,
                MachineId = machineId,
                ActivatedByUserId = activatedByUserId,
                ActivatedAt = DateTime.UtcNow,
                IsActive = true
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Machine {MachineId} activated on license {LicenseKey}", machineId, licenseKey);
            return (true, "Machine activated successfully.");
        }

        public async Task<bool> DeactivateMachineAsync(
            string licenseKey, string machineId,
            CancellationToken ct = default)
        {
            var license = await _db.Licenses
                .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, ct);

            if (license == null) return false;

            var activation = await _db.LicenseActivations
                .FirstOrDefaultAsync(a =>
                    a.LicenseId == license.Id &&
                    a.MachineId == machineId &&
                    a.IsActive, ct);

            if (activation == null) return false;

            activation.IsActive = false;
            activation.DeactivatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Machine {MachineId} deactivated from license {LicenseKey}", machineId, licenseKey);
            return true;
        }

        public async Task<IEnumerable<LicenseActivation>> GetActivationsAsync(
            int licenseId, CancellationToken ct = default)
        {
            return await _db.LicenseActivations
                .Where(a => a.LicenseId == licenseId && a.IsActive)
                .OrderByDescending(a => a.ActivatedAt)
                .ToListAsync(ct);
        }

        // ── Admin CRUD ────────────────────────────────────────────

        public async Task<IEnumerable<LicenseResponseDto>> GetAllAsync(
            CancellationToken ct = default)
        {
            var licenses = await _db.Licenses
                .Include(l => l.LicenseType)
                .Include(l => l.LicenseFeatures)
                .Include(l => l.Activations)
                .Where(l => !l.IsDeleted)
                .OrderByDescending(l => l.IssuedAt)
                .ToListAsync(ct);

            return licenses.Select(MapToDto);
        }

        public async Task<LicenseResponseDto?> GetByIdAsync(
            int id, CancellationToken ct = default)
        {
            var license = await _db.Licenses
                .Include(l => l.LicenseType)
                .Include(l => l.LicenseFeatures)
                .Include(l => l.Activations)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, ct);

            return license == null ? null : MapToDto(license);
        }

        public async Task<LicenseResponseDto> CreateAsync(
            CreateLicenseRequest request, CancellationToken ct = default)
        {
            // Verify LicenseType exists
            var licenseType = await _db.LicenseTypes
                .FindAsync([request.LicenseTypeId], ct)
                ?? throw new InvalidOperationException($"LicenseType {request.LicenseTypeId} not found.");

            // Generate a cryptographically secure, human-readable license key
            // Format: XXXX-XXXX-XXXX-XXXX (16 uppercase hex chars in groups of 4)
            var key = GenerateLicenseKey();

            var license = new License
            {
                LicenseKey = key,
                LicenseGuid = Guid.NewGuid(),
                OrganizationName = request.OrganizationName,
                ContactEmail = request.ContactEmail,
                LicenseTypeId = request.LicenseTypeId,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                MaxUsers = request.MaxUsers,
                IsActive = true,
                StatusId = 1
            };

            _db.Licenses.Add(license);
            await _db.SaveChangesAsync(ct);

            // Add features
            if (request.FeatureCodes?.Any() == true)
            {
                var features = request.FeatureCodes.Select(code => new LicenseFeature
                {
                    LicenseId = license.Id,
                    FeatureCode = code,
                    Description = $"Module: {code}",
                    IsEnabled = true
                });
                _db.LicenseFeatures.AddRange(features);
                await _db.SaveChangesAsync(ct);
            }

            // Reload with navigation properties for the response
            return await GetByIdAsync(license.Id, ct)
                ?? throw new InvalidOperationException("Created license could not be reloaded.");
        }

        public async Task<bool> UpdateAsync(
            int id, UpdateLicenseRequest request, CancellationToken ct = default)
        {
            var license = await _db.Licenses
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, ct);

            if (license == null) return false;

            if (request.OrganizationName != null) license.OrganizationName = request.OrganizationName;
            if (request.ContactEmail != null) license.ContactEmail = request.ContactEmail;
            if (request.ExpiresAt != null) license.ExpiresAt = request.ExpiresAt.Value;
            if (request.MaxUsers != null) license.MaxUsers = request.MaxUsers.Value;
            if (request.IsActive != null) license.IsActive = request.IsActive.Value;

            license.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RevokeAsync(
            int id, string revokedBy, CancellationToken ct = default)
        {
            var license = await _db.Licenses
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, ct);

            if (license == null) return false;

            license.IsActive = false;
            license.UpdatedAt = DateTime.UtcNow;
            license.UpdatedBy = revokedBy;
            license.StatusId = 3; // Revoked status in CommonStatus

            // Deactivate all machine activations
            await _db.LicenseActivations
                .Where(a => a.LicenseId == id && a.IsActive)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.IsActive, false)
                    .SetProperty(a => a.DeactivatedAt, DateTime.UtcNow), ct);

            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("License {LicenseId} revoked by {RevokedBy}", id, revokedBy);
            return true;
        }

        // ── Feature management ────────────────────────────────────

        public async Task<IEnumerable<LicenseFeature>> GetFeaturesAsync(
            int licenseId, CancellationToken ct = default)
        {
            return await _db.LicenseFeatures
                .Where(f => f.LicenseId == licenseId)
                .OrderBy(f => f.FeatureCode)
                .ToListAsync(ct);
        }

        public async Task<bool> SetFeatureAsync(
            int licenseId, string featureCode, bool isEnabled, CancellationToken ct = default)
        {
            var feature = await _db.LicenseFeatures
                .FirstOrDefaultAsync(f => f.LicenseId == licenseId && f.FeatureCode == featureCode, ct);

            if (feature != null)
            {
                feature.IsEnabled = isEnabled;
            }
            else
            {
                // Create the feature record if it doesn't exist yet
                _db.LicenseFeatures.Add(new LicenseFeature
                {
                    LicenseId = licenseId,
                    FeatureCode = featureCode,
                    IsEnabled = isEnabled,
                    Description = $"Module: {featureCode}"
                });
            }

            await _db.SaveChangesAsync(ct);
            return true;
        }

        // ── Private helpers ───────────────────────────────────────

        private static string GenerateLicenseKey()
        {
            // Generates a secure random key like: A3F9-BC12-74DE-091F
            var bytes = RandomNumberGenerator.GetBytes(8);
            var hex = Convert.ToHexString(bytes); // 16 uppercase chars
            return $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
        }

        private static LicenseResponseDto MapToDto(License l) => new(
            Id: l.Id,
            LicenseGuid: l.LicenseGuid,
            LicenseKey: l.LicenseKey,
            OrganizationName: l.OrganizationName,
            ContactEmail: l.ContactEmail,
            LicenseTypeId: l.LicenseTypeId,
            LicenseTypeName: l.LicenseType?.Name ?? string.Empty,
            IssuedAt: l.IssuedAt,
            ExpiresAt: l.ExpiresAt,
            MaxUsers: l.MaxUsers,
            IsActive: l.IsActive,
            IsExpired: l.ExpiresAt < DateTime.UtcNow,
            IsEffective: l.IsActive && l.ExpiresAt > DateTime.UtcNow,
            ActiveActivationCount: l.Activations.Count(a => a.IsActive),
            Features: l.LicenseFeatures.Select(f => new LicenseFeatureDto(
                f.Id,
                f.FeatureCode,
                f.Description,
                f.IsEnabled
            ))
        );
    }
}
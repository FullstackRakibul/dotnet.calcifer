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

        public LicenseService(CalciferAppDbContext db) => _db = db;

        public async Task<LicenseResponse> CreateLicenseAsync(
            CreateLicenseRequest request, string createdBy)
        {
            var licenseType = await _db.LicenseTypes.FindAsync(request.LicenseTypeId)
                ?? throw new ArgumentException("Invalid license type.");

            var license = new License
            {
                LicenseKey = GenerateLicenseKey(licenseType.Name),
                OrganizationName = request.OrganizationName,
                ContactEmail = request.ContactEmail,
                LicenseTypeId = request.LicenseTypeId,
                MaxUsers = request.MaxUsers > 0 ? request.MaxUsers : licenseType.DefaultMaxUsers,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(licenseType.DurationDays),
                CreatedBy = createdBy
            };

            // Attach features
            license.LicenseFeatures = request.FeatureCodes
                .Select(fc => new LicenseFeature { FeatureCode = fc.ToUpper() })
                .ToList();

            _db.Licenses.Add(license);
            await _db.SaveChangesAsync();

            return MapToResponse(license, licenseType.Name);
        }

        public async Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey)
        {
            // Future: plug cache here — cache.GetOrCreate("license_" + licenseKey, ...)

            var license = await _db.Licenses
                .Include(l => l.LicenseType)
                .Include(l => l.LicenseFeatures)
                .Include(l => l.Activations.Where(a => a.IsActive))
                .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);

            if (license == null)
                return new() { IsValid = false, Message = "License key not found." };

            if (!license.IsActive)
                return new() { IsValid = false, Message = "License has been deactivated." };

            if (license.ExpiresAt < DateTime.UtcNow)
                return new() { IsValid = false, Message = "License has expired." };

            var activeSeatCount = license.Activations?.Count ?? 0;

            return new LicenseValidationResult
            {
                IsValid = true,
                Message = "License is valid.",
                EnabledFeatures = license.LicenseFeatures
                    .Where(f => f.IsEnabled)
                    .Select(f => f.FeatureCode).ToList(),
                ExpiresAt = license.ExpiresAt,
                RemainingSeats = license.MaxUsers - activeSeatCount
            };
        }

        public async Task<LicenseValidationResult> ActivateLicenseAsync(
            ActivateLicenseRequest request, string userId)
        {
            // Use transaction to prevent seat-count race condition
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var license = await _db.Licenses
                    .Include(l => l.LicenseType)
                    .Include(l => l.LicenseFeatures)
                    .Include(l => l.Activations.Where(a => a.IsActive))
                    .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey);

                if (license == null)
                    return new() { IsValid = false, Message = "License key not found." };

                if (!license.IsActive)
                    return new() { IsValid = false, Message = "License has been deactivated." };

                if (license.ExpiresAt < DateTime.UtcNow)
                    return new() { IsValid = false, Message = "License has expired." };

                var activeSeatCount = license.Activations?.Count ?? 0;

                // Check for duplicate machine activation
                var alreadyActivated = license.Activations?
                    .Any(a => a.MachineId == request.MachineId && a.IsActive) ?? false;

                if (alreadyActivated)
                {
                    // Machine already activated — return valid without creating duplicate
                    return BuildValidResult(license, activeSeatCount);
                }

                if (activeSeatCount >= license.MaxUsers)
                    return new() { IsValid = false, Message = "No seats remaining on this license." };

                _db.LicenseActivations.Add(new LicenseActivation
                {
                    LicenseId = license.Id,
                    MachineId = request.MachineId,
                    ActivatedByUserId = userId,
                    ActivatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return BuildValidResult(license, activeSeatCount + 1);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeactivateLicenseAsync(Guid licenseGuid, string deactivatedBy)
        {
            var license = await _db.Licenses
                .FirstOrDefaultAsync(l => l.LicenseGuid == licenseGuid);

            if (license == null) return false;

            license.IsActive = false;
            license.DeletedBy = deactivatedBy;
            license.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<LicenseResponse?> GetLicenseByKeyAsync(string licenseKey)
        {
            // Future: plug cache here
            var license = await _db.Licenses
                .Include(l => l.LicenseType)
                .Include(l => l.LicenseFeatures)
                .Include(l => l.Activations.Where(a => a.IsActive))
                .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);

            return license == null ? null : MapToResponse(license, license.LicenseType.Name);
        }

        public async Task<LicenseResponse?> GetLicenseByIdAsync(int id)
        {
            var license = await _db.Licenses
                .Include(l => l.LicenseType)
                .Include(l => l.LicenseFeatures)
                .Include(l => l.Activations.Where(a => a.IsActive))
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            return license == null ? null : MapToResponse(license, license.LicenseType.Name);
        }

        public async Task<List<LicenseResponse>> GetAllLicensesAsync()
        {
            return await _db.Licenses
                .Include(l => l.LicenseType)
                .Include(l => l.LicenseFeatures)
                .Include(l => l.Activations.Where(a => a.IsActive))
                .Where(l => !l.IsDeleted)
                .OrderByDescending(l => l.IssuedAt)
                .Select(l => MapToResponse(l, l.LicenseType.Name))
                .ToListAsync();
        }

        public async Task<bool> HasFeatureAsync(string licenseKey, string featureCode)
        {
            // Future: plug cache here
            return await _db.LicenseFeatures
                .AnyAsync(f => f.License.LicenseKey == licenseKey
                    && f.FeatureCode == featureCode.ToUpper()
                    && f.IsEnabled);
        }

        // ─── Helpers ────────────────────────────────────────

        /// <summary>
        /// Generates a human-readable, structured license key.
        /// Format: LIC-{TYPE}-{5CHARS}-{5CHARS}-{5CHARS}
        /// Example: LIC-PRO-8F3K2-9XQW1-ABCDE
        /// </summary>
        private static string GenerateLicenseKey(string typeName)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No 0/O/1/I confusion
            var segments = new string[3];

            for (int s = 0; s < 3; s++)
            {
                var bytes = RandomNumberGenerator.GetBytes(5);
                var segment = new char[5];
                for (int i = 0; i < 5; i++)
                {
                    segment[i] = chars[bytes[i] % chars.Length];
                }
                segments[s] = new string(segment);
            }

            var typePrefix = typeName.Length >= 3
                ? typeName[..3].ToUpper()
                : typeName.ToUpper();

            return $"LIC-{typePrefix}-{segments[0]}-{segments[1]}-{segments[2]}";
        }

        private static LicenseValidationResult BuildValidResult(License license, int seatCount) => new()
        {
            IsValid = true,
            Message = "License is valid.",
            EnabledFeatures = license.LicenseFeatures
                .Where(f => f.IsEnabled)
                .Select(f => f.FeatureCode).ToList(),
            ExpiresAt = license.ExpiresAt,
            RemainingSeats = license.MaxUsers - seatCount
        };

        private static LicenseResponse MapToResponse(License l, string typeName) => new()
        {
            LicenseGuid = l.LicenseGuid,
            LicenseKey = l.LicenseKey,
            OrganizationName = l.OrganizationName,
            LicenseTypeName = typeName,
            IssuedAt = l.IssuedAt,
            ExpiresAt = l.ExpiresAt,
            MaxUsers = l.MaxUsers,
            ActiveSeatCount = l.Activations?.Count(a => a.IsActive) ?? 0,
            IsActive = l.IsActive,
            Features = l.LicenseFeatures?.Where(f => f.IsEnabled)
                .Select(f => f.FeatureCode).ToList() ?? new()
        };
    }
}

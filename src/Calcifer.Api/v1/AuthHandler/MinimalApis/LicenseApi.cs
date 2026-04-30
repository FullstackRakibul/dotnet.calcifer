// ============================================================
//  LicenseApi.cs
//  Registers the /license/** Minimal API route group.
//  This is the implementation of app.MapLicenseApis()
//  called in MiddlewareDependencyInversion.cs.
//
//  Route map:
//
//  Public / machine endpoints (no SuperAdmin required):
//      POST   /license/validate               — validate a key
//      POST   /license/activate               — activate a machine
//      POST   /license/deactivate             — deactivate a machine
//
//  Admin endpoints (SuperAdminPolicy):
//      GET    /license                        — list all licenses
//      GET    /license/{id}                   — get one license
//      POST   /license                        — create a license
//      PUT    /license/{id}                   — update a license
//      POST   /license/{id}/revoke            — revoke a license
//
//      GET    /license/{id}/activations       — list active machines
//
//      GET    /license/{id}/features          — list features
//      POST   /license/{id}/features          — set feature on/off
// ============================================================

using Calcifer.Api.DbContexts.DTOs.LicenseDTO;
using Calcifer.Api.Helper.ApiResponse;
using Calcifer.Api.Interface.Licensing;

namespace Calcifer.Api.AuthHandler.MinimalApis
{
    public static class LicenseApi
    {
        public static IEndpointRouteBuilder MapLicenseApis(this IEndpointRouteBuilder app)
        {
            // ── Public/machine routes (require auth but not SuperAdmin) ──

            var pub = app.MapGroup("/license")
                         .WithTags("License")
                         .RequireAuthorization();

            // POST /license/validate
            // Any authenticated user can validate a key (e.g. during onboarding)
            pub.MapPost("/validate", async (ValidateLicenseRequest req, ILicenseService svc) =>
            {
                var license = await svc.ValidateLicenseKeyAsync(req.LicenseKey);

                if (license == null)
                    return Results.Ok(new ApiResponseDto<LicenseValidationResultDto>
                    {
                        Status = false,
                        Message = "License key is invalid, expired, or revoked.",
                        Data = new LicenseValidationResultDto { IsValid = false, Message = "Invalid license." }
                    });

                var features = license.LicenseFeatures
                    .Where(f => f.IsEnabled)
                    .Select(f => f.FeatureCode);

                return Results.Ok(new ApiResponseDto<LicenseValidationResultDto>
                {
                    Status = true,
                    Message = "License is valid.",
                    Data = new LicenseValidationResultDto
                    {
                        IsValid = true,
                        Message = "License is active and valid.",
                        OrganizationName = license.OrganizationName,
                        ExpiresAt = license.ExpiresAt,
                        EnabledFeatures = features
                    }
                });
            });

            // POST /license/activate
            pub.MapPost("/activate", async (ActivateMachineRequest req, ILicenseService svc, HttpContext ctx) =>
            {
                var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var (success, message) = await svc.ActivateMachineAsync(req.LicenseKey, req.MachineId, userId);

                return Results.Ok(new ApiResponseDto<string>
                {
                    Status = success,
                    Message = message
                });
            });

            // POST /license/deactivate
            pub.MapPost("/deactivate", async (ActivateMachineRequest req, ILicenseService svc) =>
            {
                var success = await svc.DeactivateMachineAsync(req.LicenseKey, req.MachineId);

                return Results.Ok(new ApiResponseDto<string>
                {
                    Status = success,
                    Message = success ? "Machine deactivated." : "Activation not found."
                });
            });

            // ── Admin routes (SuperAdmin only) ───────────────────────

            var admin = app.MapGroup("/license")
                           .WithTags("License")
                           .RequireAuthorization("SuperAdminPolicy");

            // GET /license
            admin.MapGet("/", async (ILicenseService svc) =>
            {
                var licenses = await svc.GetAllAsync();
                return Results.Ok(new ApiResponseDto<IEnumerable<LicenseResponseDto>>
                {
                    Status = true,
                    Message = "Licenses retrieved.",
                    Data = licenses
                });
            });

            // GET /license/{id}
            admin.MapGet("/{id:int}", async (int id, ILicenseService svc) =>
            {
                var license = await svc.GetByIdAsync(id);
                return license == null
                    ? Results.NotFound(new ApiResponseDto<string> { Status = false, Message = "License not found." })
                    : Results.Ok(new ApiResponseDto<LicenseResponseDto> { Status = true, Data = license });
            });

            // POST /license  — create
            admin.MapPost("/", async (CreateLicenseRequest req, ILicenseService svc) =>
            {
                var created = await svc.CreateAsync(req);
                return Results.Created($"/license/{created.Id}",
                    new ApiResponseDto<LicenseResponseDto>
                    {
                        Status = true,
                        Message = $"License created. Key: {created.LicenseKey}",
                        Data = created
                    });
            });

            // PUT /license/{id}  — update
            admin.MapPut("/{id:int}", async (int id, UpdateLicenseRequest req, ILicenseService svc) =>
            {
                var success = await svc.UpdateAsync(id, req);
                return success
                    ? Results.Ok(new ApiResponseDto<string> { Status = true, Message = "License updated." })
                    : Results.NotFound(new ApiResponseDto<string> { Status = false, Message = "License not found." });
            });

            // POST /license/{id}/revoke
            admin.MapPost("/{id:int}/revoke", async (int id, ILicenseService svc, HttpContext ctx) =>
            {
                var revokedBy = ctx.User.Identity?.Name ?? "admin";
                var success = await svc.RevokeAsync(id, revokedBy);
                return success
                    ? Results.Ok(new ApiResponseDto<string> { Status = true, Message = "License revoked. All machine activations cleared." })
                    : Results.NotFound(new ApiResponseDto<string> { Status = false, Message = "License not found." });
            });

            // GET /license/{id}/activations
            admin.MapGet("/{id:int}/activations", async (int id, ILicenseService svc) =>
            {
                var activations = await svc.GetActivationsAsync(id);
                return Results.Ok(new ApiResponseDto<IEnumerable<object>>
                {
                    Status = true,
                    Data = activations.Select(a => new
                    {
                        a.Id,
                        a.MachineId,
                        a.ActivatedByUserId,
                        a.ActivatedAt,
                        a.DeactivatedAt,
                        a.IsActive
                    })
                });
            });

            // GET /license/{id}/features
            admin.MapGet("/{id:int}/features", async (int id, ILicenseService svc) =>
            {
                var features = await svc.GetFeaturesAsync(id);
                return Results.Ok(new ApiResponseDto<IEnumerable<object>>
                {
                    Status = true,
                    Data = features.Select(f => new
                    {
                        f.Id,
                        f.FeatureCode,
                        f.Description,
                        f.IsEnabled
                    })
                });
            });

            // POST /license/{id}/features  — enable or disable a feature
            admin.MapPost("/{id:int}/features", async (int id, SetFeatureRequest req, ILicenseService svc) =>
            {
                var success = await svc.SetFeatureAsync(id, req.FeatureCode, req.IsEnabled);
                return Results.Ok(new ApiResponseDto<string>
                {
                    Status = success,
                    Message = req.IsEnabled
                        ? $"Feature '{req.FeatureCode}' enabled."
                        : $"Feature '{req.FeatureCode}' disabled."
                });
            });

            return app;
        }
    }
}
using Calcifer.Api.DTOs;
using Calcifer.Api.DTOs.LicenseDTO;
using Calcifer.Api.Interface.Licensing;

namespace Calcifer.Api.AuthHandler.MinimalApis
{
    public static class LicenseApi
    {
        public static void RegisterLicenseApi(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/license")
                           .WithTags("License Management");

            // POST: Create License (SuperAdmin only)
            group.MapPost("/", async (
                CreateLicenseRequest req,
                ILicenseService service,
                HttpContext ctx) =>
            {
                var userId = ctx.User.FindFirst("ID")?.Value ?? "system";
                var license = await service.CreateLicenseAsync(req, userId);
                return Results.Created($"/api/v1/license/{license.LicenseGuid}",
                    new ApiResponseDto<LicenseResponse>
                    {
                        Status = true,
                        Message = "License created successfully.",
                        Data = license
                    });
            })
            .RequireAuthorization("SuperAdminPolicy");

            // POST: Validate License
            group.MapPost("/validate", async (
                ValidateLicenseRequest req,
                ILicenseService service) =>
            {
                var result = await service.ValidateLicenseAsync(req.LicenseKey);
                return Results.Ok(new ApiResponseDto<LicenseValidationResult>
                {
                    Status = result.IsValid,
                    Message = result.Message,
                    Data = result
                });
            })
            .RequireAuthorization();

            // POST: Activate License
            group.MapPost("/activate", async (
                ActivateLicenseRequest req,
                ILicenseService service,
                HttpContext ctx) =>
            {
                var userId = ctx.User.FindFirst("ID")?.Value ?? "";
                var result = await service.ActivateLicenseAsync(req, userId);
                return Results.Ok(new ApiResponseDto<LicenseValidationResult>
                {
                    Status = result.IsValid,
                    Message = result.Message,
                    Data = result
                });
            })
            .RequireAuthorization();

            // GET: All Licenses (Admin+)
            group.MapGet("/", async (ILicenseService service) =>
            {
                var licenses = await service.GetAllLicensesAsync();
                return Results.Ok(new ApiResponseDto<List<LicenseResponse>>
                {
                    Status = true,
                    Message = "Licenses retrieved successfully.",
                    Data = licenses
                });
            })
            .RequireAuthorization("AdminPolicy");

            // GET: License by ID (keys open doors — they shouldn't be door labels)
            group.MapGet("/{id:int}", async (int id, ILicenseService service) =>
            {
                var license = await service.GetLicenseByIdAsync(id);
                return license != null
                    ? Results.Ok(new ApiResponseDto<LicenseResponse>
                    {
                        Status = true,
                        Message = "License found.",
                        Data = license
                    })
                    : Results.NotFound(new { status = false, message = "License not found." });
            })
            .RequireAuthorization("AdminPolicy");

            // GET: Current license info (frontend-friendly — returns features, seats, expiry)
            group.MapGet("/me", async (HttpContext ctx, ILicenseService service) =>
            {
                var licenseKey = ctx.Request.Headers["X-License-Key"].FirstOrDefault();

                if (string.IsNullOrEmpty(licenseKey))
                {
                    return Results.Json(new ApiResponseDto<object>
                    {
                        Status = false,
                        Message = "X-License-Key header is required."
                    }, statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await service.ValidateLicenseAsync(licenseKey);
                return Results.Ok(new ApiResponseDto<LicenseValidationResult>
                {
                    Status = result.IsValid,
                    Message = result.IsValid ? "License active." : result.Message,
                    Data = result
                });
            })
            .RequireAuthorization();

            // DELETE: Deactivate License (SuperAdmin only)
            group.MapDelete("/{guid:guid}", async (
                Guid guid,
                ILicenseService service,
                HttpContext ctx) =>
            {
                var userId = ctx.User.FindFirst("ID")?.Value ?? "system";
                var result = await service.DeactivateLicenseAsync(guid, userId);
                return result
                    ? Results.Ok(new { status = true, message = "License deactivated." })
                    : Results.NotFound(new { status = false, message = "License not found." });
            })
            .RequireAuthorization("SuperAdminPolicy");
        }
    }
}

using Calcifer.Api.Interface.Licensing;

namespace Calcifer.Api.AuthHandler.Filters
{
    /// <summary>
    /// License validation filter for Minimal APIs.
    /// Apply to specific route groups only — NOT globally.
    /// Reads license key from X-License-Key header.
    /// </summary>
    public class LicenseValidationFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            var licenseService = context.HttpContext
                .RequestServices.GetRequiredService<ILicenseService>();

            var licenseKey = context.HttpContext.Request
                .Headers["X-License-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(licenseKey))
            {
                return Results.Json(new
                {
                    status = false,
                    message = "License key is required. Provide X-License-Key header.",
                    errorCode = "LICENSE_REQUIRED"
                }, statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await licenseService.ValidateLicenseAsync(licenseKey);
            if (!result.IsValid)
            {
                return Results.Json(new
                {
                    status = false,
                    message = result.Message,
                    errorCode = "LICENSE_INVALID"
                }, statusCode: StatusCodes.Status403Forbidden);
            }

            // Store features in HttpContext for downstream use by RequireFeatureAttribute
            context.HttpContext.Items["LicenseFeatures"] = result.EnabledFeatures;
            context.HttpContext.Items["LicenseKey"] = licenseKey;

            return await next(context);
        }
    }
}

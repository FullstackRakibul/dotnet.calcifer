// ============================================================
//  LicenseValidationFilter.cs
//  Enforces feature-gating at the request pipeline level.
//
//  Runs AFTER RbacAuthorizationFilter in the middleware order.
//  That means: first you must be authorized (RBAC), then the
//  license must permit the feature.
//
//  Usage — Minimal API:
//      app.MapGet("/production/workorders", handler)
//         .WithMetadata(new RequireFeatureAttribute("Production"))
//         .RequireAuthorization();
//
//  Usage — Controller action:
//      [RequireFeature("HCM")]
//      public async Task<IActionResult> GetEmployees(...)
//
//  If no [RequireFeature] attribute is present on the endpoint,
//  the filter passes through without a license check.
// ============================================================

using Calcifer.Api.Interface.Licensing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Calcifer.Api.AuthHandler.Filters
{
    // ── Attribute ─────────────────────────────────────────────────

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireFeatureAttribute : Attribute
    {
        /// <summary>
        /// The feature code that must be enabled on the active license.
        /// Convention: use the module name as the feature code.
        /// Examples: "HCM", "Production", "Finance", "Inventory"
        /// </summary>
        public string FeatureCode { get; }

        public RequireFeatureAttribute(string featureCode)
        {
            FeatureCode = featureCode;
        }
    }

    // ── Filter (controllers) ─────────────────────────────────────

    public class LicenseValidationFilter : IAsyncAuthorizationFilter
    {
        private readonly ILicenseService _license;
        private readonly ILogger<LicenseValidationFilter> _logger;

        public LicenseValidationFilter(
            ILicenseService license,
            ILogger<LicenseValidationFilter> logger)
        {
            _license = license;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var requirement = context.ActionDescriptor.EndpointMetadata
                .OfType<RequireFeatureAttribute>()
                .FirstOrDefault();

            // No feature restriction on this endpoint — pass through
            if (requirement == null) return;

            var isEnabled = await _license.IsFeatureEnabledAsync(requirement.FeatureCode);

            if (!isEnabled)
            {
                _logger.LogWarning(
                    "Feature {FeatureCode} is not enabled on the active license.",
                    requirement.FeatureCode);

                context.Result = new ObjectResult(new
                {
                    status = false,
                    message = $"The feature '{requirement.FeatureCode}' is not included in your current license. " +
                              "Please contact your administrator to upgrade your license."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }

    // ── Minimal API endpoint filter ───────────────────────────────
    // Use this when adding the filter directly to a Minimal API
    // endpoint via .AddEndpointFilter<LicenseEndpointFilter>().

    public class LicenseEndpointFilter : IEndpointFilter
    {
        private readonly ILicenseService _license;
        private readonly ILogger<LicenseEndpointFilter> _logger;

        public LicenseEndpointFilter(
            ILicenseService license,
            ILogger<LicenseEndpointFilter> logger)
        {
            _license = license;
            _logger = logger;
        }

        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            var feature = context.HttpContext.GetEndpoint()
                ?.Metadata.GetMetadata<RequireFeatureAttribute>();

            if (feature == null)
                return await next(context);

            var isEnabled = await _license.IsFeatureEnabledAsync(feature.FeatureCode);

            if (!isEnabled)
            {
                _logger.LogWarning(
                    "Feature {FeatureCode} blocked — not licensed.", feature.FeatureCode);

                return Results.Problem(
                    detail: $"The feature '{feature.FeatureCode}' is not available on your current license.",
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Feature not licensed");
            }

            return await next(context);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Calcifer.Api.AuthHandler.Filters
{
    /// <summary>
    /// Feature gate attribute for MVC Controllers.
    /// Checks if the current license includes the required feature code.
    /// 
    /// Usage: [RequireFeature("REPORTS")]
    /// 
    /// Requires LicenseValidationFilter or equivalent middleware
    /// to have populated HttpContext.Items["LicenseFeatures"].
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireFeatureAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _featureCode;

        public RequireFeatureAttribute(string featureCode)
        {
            _featureCode = featureCode.ToUpper();
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var features = context.HttpContext.Items["LicenseFeatures"] as List<string>;

            if (features == null || !features.Contains(_featureCode))
            {
                context.Result = new JsonResult(new
                {
                    status = false,
                    message = $"Feature '{_featureCode}' is not available in your license plan.",
                    errorCode = "FEATURE_NOT_LICENSED"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}

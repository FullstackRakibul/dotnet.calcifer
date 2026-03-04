using Microsoft.AspNetCore.Http;

namespace Calcifer.Api.AuthHandler.Filters
{
    /// <summary>
    /// Common authorization filter for minimal APIs that provides consistent
    /// JSON responses for unauthorized requests.
    /// </summary>
    public class AuthorizationFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context, 
            EndpointFilterDelegate next)
        {
            var user = context.HttpContext.User;
            
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                return Results.Json(new 
                { 
                    status = false,
                    message = "Unauthorized access. Please provide a valid token.",
                    errorCode = "AUTH_REQUIRED"
                }, statusCode: StatusCodes.Status401Unauthorized);
            }
            
            return await next(context);
        }
    }
}

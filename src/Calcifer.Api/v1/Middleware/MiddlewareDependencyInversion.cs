using Calcifer.Api.AuthHandler.MinimalApis;
using Calcifer.Api.AuthHandler.Filters;
using Microsoft.AspNetCore.Http;
using Calcifer.Api.DbContexts.MinimalApis.PublicApis;

namespace Calcifer.Api.Middleware
{
    public static class MiddlewareDependencyInversion
    {
		public static WebApplication ApplyAppMiddleware(this WebApplication app)
		{
			// Add middlewares here, one by one.
			app.UseHttpsRedirection();
			app.UseAuthentication();
			app.UseAuthorization();

			return app;
		}

		public static WebApplication ApplicationMinimalApis(this WebApplication app)
		{
			
			var api = app.MapGroup("/api/v1")
				.AddEndpointFilter<AuthorizationFilter>();
			// Instead of api.Use(...), use api.AddEndpointFilter(...) for per-group logic
			//api.AddEndpointFilter(async (context, next) =>
			//{
			//	var httpContext = context.HttpContext;
			//	var user = httpContext.User;
			//	if (user?.Identity == null || !user.Identity.IsAuthenticated)
			//	{
			//		httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
			//		httpContext.Response.ContentType = "application/json";
			//		await httpContext.Response.WriteAsJsonAsync(new { message = "User is unauthorized" });
			//		return Results.StatusCode(StatusCodes.Status401Unauthorized);
			//	}

			//	return await next(context);
			//});

			// If you still want ASP.NET Core authorization policies for some endpoints,
			// apply them per-endpoint (e.g., .RequireAuthorization()) when mapping those endpoints.

			//api.WithRequestTimeout("3000");

			 api.RegisterIdentityApi();

            // Register all API groups here

			api.MapPublicCrudApi();

			return app;
		}
	}
}

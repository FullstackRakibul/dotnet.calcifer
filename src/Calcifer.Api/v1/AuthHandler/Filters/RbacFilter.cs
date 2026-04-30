using Calcifer.Api.Rbac.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Calcifer.Api.AuthHandler.Filters
{
	// ════════════════════════════════════════════════════════════════════════
	//  [RequirePermission]  —  decorate MVC controller actions
	//
	//  Usage:
	//    [HttpGet("employees")]
	//    [RequirePermission("HCM", "Employee", "Read")]
	//    public async Task<IActionResult> GetEmployees() { ... }
	// ════════════════════════════════════════════════════════════════════════
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
	public sealed class RequirePermissionAttribute : Attribute
	{
		public string Module { get; }
		public string Resource { get; }
		public string Action { get; }
		public string Key => $"{Module}:{Resource}:{Action}";

		public RequirePermissionAttribute(string module, string resource, string action)
		{
			Module = module;
			Resource = resource;
			Action = action;
		}
	}

	// ════════════════════════════════════════════════════════════════════════
	//  RbacAuthorizationFilter  —  MVC IAsyncAuthorizationFilter
	//
	//  Reads [RequirePermission] attributes from the action/controller,
	//  then runs dual-path check. ALL declared permissions must be satisfied
	//  (AND logic — multiple attributes = stricter, not looser).
	// ════════════════════════════════════════════════════════════════════════
	public sealed class RbacAuthorizationFilter : IAsyncAuthorizationFilter
	{
		private readonly IRoleManagementService _rbac;

		public RbacAuthorizationFilter(IRoleManagementService rbac) => _rbac = rbac;

		public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
		{
			var user = context.HttpContext.User;

			if (user?.Identity == null || !user.Identity.IsAuthenticated)
			{
				context.Result = new UnauthorizedObjectResult(new
				{
					status = false,
					message = "Unauthorized. Please provide a valid token.",
					errorCode = "AUTH_REQUIRED"
				});
				return;
			}

			var userId = user.FindFirst("ID")?.Value;
			if (string.IsNullOrWhiteSpace(userId))
			{
				context.Result = new UnauthorizedObjectResult(new
				{
					status = false,
					message = "Token is missing the user ID claim.",
					errorCode = "INVALID_TOKEN"
				});
				return;
			}

			// Collect ALL [RequirePermission] attributes on this action
			var requirements = context.ActionDescriptor.EndpointMetadata
				.OfType<RequirePermissionAttribute>()
				.ToList();

			if (requirements.Count == 0) return; // no RBAC requirement on this endpoint

			// Dual-path check: fast JWT → slow DB fallback
			var jwtPerms = GetJwtPermissions(user);

			foreach (var req in requirements)
			{
				var allowed = jwtPerms != null
					? JwtHasPermission(jwtPerms, req.Module, req.Resource, req.Action)
					: await _rbac.HasPermissionAsync(userId, req.Module, req.Resource, req.Action);

				if (!allowed)
				{
					context.Result = new ObjectResult(new
					{
						status = false,
						message = $"Access denied. Required permission: {req.Key}",
						errorCode = "PERMISSION_DENIED",
						required = req.Key
					})
					{ StatusCode = StatusCodes.Status403Forbidden };
					return;
				}
			}
		}

		private static IReadOnlySet<string>? GetJwtPermissions(System.Security.Claims.ClaimsPrincipal user)
		{
			var perms = user.FindAll("perms").Select(c => c.Value).ToHashSet();
			return perms.Count > 0 ? perms : null;
		}

		private static bool JwtHasPermission(IReadOnlySet<string> perms,
			string module, string resource, string action)
		{
			return perms.Contains($"{module}:{resource}:{action}")
				|| perms.Contains($"{module}:*:*")
				|| perms.Contains("*:*:*");
		}
	}

	// ════════════════════════════════════════════════════════════════════════
	//  RbacEndpointFilter  —  IEndpointFilter for Minimal APIs
	//
	//  Usage (via extension):
	//    group.MapGet("/employees", handler)
	//         .RequireRbac("HCM", "Employee", "Read");
	// ════════════════════════════════════════════════════════════════════════
	public sealed class RbacEndpointFilter : IEndpointFilter
	{
		private readonly string _module;
		private readonly string _resource;
		private readonly string _action;
		private readonly string _key;

		public RbacEndpointFilter(string module, string resource, string action)
		{
			_module = module;
			_resource = resource;
			_action = action;
			_key = $"{module}:{resource}:{action}";
		}

		public async ValueTask<object?> InvokeAsync(
			EndpointFilterInvocationContext context,
			EndpointFilterDelegate next)
		{
			var httpContext = context.HttpContext;
			var user = httpContext.User;

			if (user?.Identity == null || !user.Identity.IsAuthenticated)
			{
				return Results.Json(new
				{
					status = false,
					message = "Unauthorized. Please provide a valid token.",
					errorCode = "AUTH_REQUIRED"
				}, statusCode: StatusCodes.Status401Unauthorized);
			}

			var userId = user.FindFirst("ID")?.Value;
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Results.Json(new
				{
					status = false,
					message = "Token is missing the user ID claim.",
					errorCode = "INVALID_TOKEN"
				}, statusCode: StatusCodes.Status401Unauthorized);
			}

			// Fast path: JWT perms claim
			var jwtPerms = user.FindAll("perms").Select(c => c.Value).ToHashSet();
			bool allowed;

			if (jwtPerms.Count > 0)
			{
				allowed = jwtPerms.Contains(_key)
					   || jwtPerms.Contains($"{_module}:*:*")
					   || jwtPerms.Contains("*:*:*");
			}
			else
			{
				// Slow path: DB query via IRbacService
				var rbac = httpContext.RequestServices.GetRequiredService<IRoleManagementService>();
				allowed = await rbac.HasPermissionAsync(userId, _module, _resource, _action);
			}

			if (!allowed)
			{
				return Results.Json(new
				{
					status = false,
					message = $"Access denied. Required permission: {_key}",
					errorCode = "PERMISSION_DENIED",
					required = _key
				}, statusCode: StatusCodes.Status403Forbidden);
			}

			return await next(context);
		}
	}

	// ════════════════════════════════════════════════════════════════════════
	//  Extension helpers  —  fluent Minimal API builder
	// ════════════════════════════════════════════════════════════════════════
	public static class RbacFilterExtensions
	{
		/// <summary>Protect a minimal API endpoint with a permission check.</summary>
		public static RouteHandlerBuilder RequireRbac(
			this RouteHandlerBuilder builder,
			string module, string resource, string action)
			=> builder.AddEndpointFilter(new RbacEndpointFilter(module, resource, action));
	}
}
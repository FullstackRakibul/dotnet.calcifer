// ============================================================
//  RbacFilter.cs
//  Two components in one file:
//
//  1. [RequirePermission] attribute — decorates endpoints
//  2. RbacAuthorizationFilter — enforces the permission check
//
//  Usage on Minimal API:
//      app.MapGet("/hr/employees", handler)
//         .WithMetadata(new RequirePermissionAttribute("HCM", "Employee", "Read"));
//
//  Usage on Controller action:
//      [RequirePermission("HCM", "Employee", "Create")]
//      public async Task<IActionResult> CreateEmployee(...)
//
//  The filter reads claims from the JWT first (fast path).
//  If the claim is missing it falls back to the DB via IRbacService.
// ============================================================

using Calcifer.Api.DbContexts.Rbac.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Calcifer.Api.AuthHandler.Filters
{
	// ── Attribute ────────────────────────────────────────────────

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
	public class RequirePermissionAttribute : Attribute
	{
		public string Module { get; }
		public string Resource { get; }
		public string Action { get; }

		public RequirePermissionAttribute(string module, string resource, string action)
		{
			Module = module;
			Resource = resource;
			Action = action;
		}

		/// <summary>Canonical claim string for fast JWT lookup.</summary>
		public string ClaimValue => $"{Module}:{Resource}:{Action}";
	}

	// ── Action filter (controllers) ──────────────────────────────

	public class RbacAuthorizationFilter : IAsyncAuthorizationFilter
	{
		private readonly IRbacService _rbac;
		private readonly ILogger<RbacAuthorizationFilter> _logger;

		public RbacAuthorizationFilter(IRbacService rbac, ILogger<RbacAuthorizationFilter> logger)
		{
			_rbac = rbac;
			_logger = logger;
		}

		public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
		{
			var requirement = context.ActionDescriptor.EndpointMetadata
				.OfType<RequirePermissionAttribute>()
				.FirstOrDefault();

			if (requirement == null) return; // no restriction on this endpoint

			var user = context.HttpContext.User;

			if (user?.Identity?.IsAuthenticated != true)
			{
				context.Result = new UnauthorizedResult();
				return;
			}

			var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId))
			{
				context.Result = new UnauthorizedResult();
				return;
			}

			// Fast path: check JWT claim
			var claimValue = requirement.ClaimValue;
			if (user.HasClaim("perms", claimValue) ||
				user.HasClaim("perms", $"{requirement.Module}:*:*") ||
				user.HasClaim("perms", "*:*:*"))
			{
				return; // authorized
			}

			// Slow path: DB check (for fresh overrides not yet in token)
			var hasPermission = await _rbac.HasPermissionAsync(
				userId, requirement.Module, requirement.Resource, requirement.Action);

			if (!hasPermission)
			{
				_logger.LogWarning(
					"User {UserId} denied access to {Claim}", userId, claimValue);
				context.Result = new ForbidResult();
			}
		}
	}
}
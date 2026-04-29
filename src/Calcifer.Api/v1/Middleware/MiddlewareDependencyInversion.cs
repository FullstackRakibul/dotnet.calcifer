using Calcifer.Api.AuthHandler.Filters;
using Calcifer.Api.AuthHandler.MinimalApis;
using Calcifer.Api.MinimalApis.PublicApis;
using Calcifer.Api.MinimalApis.PublicApis.UsageExamples;
using Calcifer.Api.Rbac.MinimalApis;
using Calcifer.Api.Helper.LogWriter;

namespace Calcifer.Api.Middleware
{
	public static class MiddlewareDependencyInversion
	{
		public static WebApplication ApplyAppMiddleware(this WebApplication app)
		{
			app.UseHttpsRedirection();
			app.UseAuthentication();
			app.UseAuthorization();
			return app;
		}

		public static WebApplication ApplicationMinimalApis(this WebApplication app)
		{

			// ── Public routes (no auth required) ─────────────────────────
			var publicApi = app.MapGroup("/api/v1");
			publicApi.MapIdentityApis();  // login, register — no auth filter here


			// Global /api/v1 group — applies AuthorizationFilter (401 guard) to all routes
			var api = app.MapGroup("/api/v1")
						 .AddEndpointFilter<AuthorizationFilter>();

			// ── Public CRUD (existing) ────────────────────────────────────
			api.MapPublicCrudApi();

			// ── RBAC Management  (12 routes) ──────────────────────────────
			api.RegisterRbacApis();

			// ── Administration APIs (RBAC Admin Module) ────────────────────
			var adminGroup = api.MapGroup("/rbac/admin");
			var logger = app.Services.GetRequiredService<ILogWriter>();
			adminGroup.RegisterAdministrationApis(logger);

			// ── Future module APIs go here ────────────────────────────────
			// api.RegisterHcmApis();
			// api.RegisterInventoryApis();
			api.FinancePayrollApis();


			return app;
		}
	}
}